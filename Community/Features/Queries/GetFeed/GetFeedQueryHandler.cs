using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Services.Interface;
using BreastCancer.Context;
using BreastCancer.Enum;
using BreastCancer.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace BreastCancer.Community.Features.Feed;

public sealed class GetFeedQueryHandler : IRequestHandler<GetFeedQuery, FeedResponseDto>
{
    private const string FeedKeyPrefix = "rehla:community:feed:";
    private const string PostCacheKeyPrefix = "post:";
    private static readonly TimeSpan PostCacheTtl = TimeSpan.FromHours(1);

    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly BreastCancerDB _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetFeedQueryHandler> _logger;

    public GetFeedQueryHandler(
        IConnectionMultiplexer connectionMultiplexer,
        BreastCancerDB dbContext,
        ICacheService cacheService,
        ILogger<GetFeedQueryHandler> logger)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FeedResponseDto> Handle(GetFeedQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return new FeedResponseDto();
        }

        var normalizedLimit = Math.Clamp(request.Limit, 1, 50);
        var roleFlags = RoleFlags.Create(request.Roles);
        var allowedVisibilities = GetAllowedVisibilities(roleFlags);
        var redisDb = _connectionMultiplexer.GetDatabase();
        var feedKey = BuildFeedKey(request.UserId);

        List<int>? feedIds = null;

        if (await redisDb.KeyExistsAsync(feedKey))
        {
            feedIds = await GetFeedIdsFromRedisAsync(request, normalizedLimit, redisDb, feedKey);

            if (feedIds != null)
            {
                _logger.LogInformation("Feed cache hit for user {UserId}; retrieving feed IDs from Redis.", request.UserId);
            }
            else
            {
                _logger.LogInformation("Feed cache hit, but cursor {Cursor} not found in Redis. Falling back to SQL for deep pagination.", request.Cursor);
            }
        }

        if (feedIds == null)
        {
            _logger.LogInformation("Feed cache miss for user {UserId}; falling back to SQL feed query.", request.UserId);
            feedIds = await GetFeedIdsFromSqlAsync(request, roleFlags, normalizedLimit, cancellationToken);
        }

        var hydratedPosts = await HydratePostsAsync(feedIds, roleFlags, cancellationToken);

        await AttachReactionCountsAsync(hydratedPosts, cancellationToken);

        return BuildResponse(hydratedPosts, normalizedLimit);
    }

    private async Task<List<int>?> GetFeedIdsFromRedisAsync(
            GetFeedQuery request,
            int normalizedLimit,
            IDatabase redisDb,
            string feedKey)
    {
        var fetchLimit = normalizedLimit * 2;

        var start = await GetRedisStartRankAsync(redisDb, feedKey, request.Cursor);

        if (start == null) return null;

        var batch = await redisDb.SortedSetRangeByRankAsync(feedKey, start.Value, start.Value + fetchLimit, Order.Descending);

        return batch.Select(v => (int)v).ToList();
    }

    private async Task<List<int>> GetFeedIdsFromSqlAsync(
        GetFeedQuery request,
        RoleFlags roleFlags,
        int normalizedLimit,
        CancellationToken cancellationToken)
    {
        var query = from follow in _dbContext.Follows.AsNoTracking()
                    join post in _dbContext.Posts.AsNoTracking() on follow.FollowingId equals post.AuthorId
                    where follow.FollowerId == request.UserId && !post.IsDeleted
                    select post;

        if (request.Cursor.HasValue)
        {
            var cursorInfo = await _dbContext.Posts.AsNoTracking()
                .Where(p => p.Id == request.Cursor.Value)
                .Select(p => new { p.CreatedAt, p.Id })
                .SingleOrDefaultAsync(cancellationToken);

            if (cursorInfo is not null)
            {
                query = query.Where(p => p.CreatedAt < cursorInfo.CreatedAt
                    || (p.CreatedAt == cursorInfo.CreatedAt && p.Id < cursorInfo.Id));
            }
            else
            {
                query = query.Where(p => p.Id < request.Cursor.Value);
            }
        }

        return await query
            .Where(p => p.Visibility == PostVisibility.Public
                || (roleFlags.IsDoctor && (p.Visibility == PostVisibility.DoctorOnly || p.Visibility == PostVisibility.PatientsOnly || p.Visibility == PostVisibility.CaregiverOnly))
                || (!roleFlags.IsDoctor && roleFlags.IsPatient && p.Visibility == PostVisibility.PatientsOnly)
                || (!roleFlags.IsDoctor && roleFlags.IsCaregiver && p.Visibility == PostVisibility.CaregiverOnly))
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(normalizedLimit + 1)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<PostDTO>> HydratePostsAsync(
        List<int> postIds,
        RoleFlags roleFlags,
        CancellationToken cancellationToken)
    {
        if (postIds.Count == 0) return new List<PostDTO>();

        var results = new Dictionary<int, PostDTO>(postIds.Count);
        var missedIds = new List<int>();

        // 1. Check Cache 
        foreach (var id in postIds)
        {
            var cacheKey = $"{PostCacheKeyPrefix}{id}";
            var post = await _cacheService.GetAsync<PostDTO>(cacheKey, cancellationToken);

            if (post != null)
                results[id] = post;
            else
                missedIds.Add(id);
        }

        // 2. Cache Miss - Hydrate from DB
        if (missedIds.Any())
        {
            var postsFromDb = await _dbContext.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Where(p => missedIds.Contains(p.Id) && !p.IsDeleted)
                .Select(p => new PostDTO
                {
                    Id = p.Id,
                    AuthorId = p.AuthorId,
                    Content = p.Content,
                    PostType = p.Type,
                    PostVisibility = p.Visibility,
                    MediaUrls = p.MediaUrls,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    IsEdited = p.IsEdited
                })
                .ToListAsync(cancellationToken);

            foreach (var post in postsFromDb)
            {
                var cacheKey = $"{PostCacheKeyPrefix}{post.Id}";
                await _cacheService.SetAsync(cacheKey, post, PostCacheTtl, cancellationToken);
                results[post.Id] = post;
            }

            foreach (var id in missedIds)
            {
                if (!results.ContainsKey(id))
                    _logger.LogWarning("Post {PostId} not found in database during hydration", id);
            }
        }

        var allowedVisibilities = GetAllowedVisibilities(roleFlags);
        return postIds
            .Where(id => results.ContainsKey(id) && allowedVisibilities.Contains(results[id].PostVisibility))
            .Select(id => results[id])
            .ToList();
    }

    private static FeedResponseDto BuildResponse(List<PostDTO> hydratedPosts, int normalizedLimit)
    {
        var page = hydratedPosts.Take(normalizedLimit).ToList();
        var hasMore = hydratedPosts.Count > normalizedLimit;

        return new FeedResponseDto
        {
            Posts = page,
            NextCursor = hasMore && page.Count > 0 ? page[^1].Id : null
        };
    }

    private static IReadOnlySet<PostVisibility> GetAllowedVisibilities(RoleFlags roles)
    {
        var allowed = new HashSet<PostVisibility> { PostVisibility.Public };
        if (roles.IsDoctor)
        {
            allowed.UnionWith(new[] { PostVisibility.DoctorOnly, PostVisibility.PatientsOnly, PostVisibility.CaregiverOnly });
            return allowed;
        }
        if (roles.IsPatient) allowed.Add(PostVisibility.PatientsOnly);
        if (roles.IsCaregiver) allowed.Add(PostVisibility.CaregiverOnly);
        return allowed;
    }

    private readonly record struct RoleFlags(bool IsDoctor, bool IsPatient, bool IsCaregiver)
    {
        public static RoleFlags Create(IReadOnlyCollection<string>? roles)
        {
            roles ??= Array.Empty<string>();
            return new RoleFlags(
                roles.Any(r => string.Equals(r, "Doctor", StringComparison.OrdinalIgnoreCase)),
                roles.Any(r => string.Equals(r, "Patient", StringComparison.OrdinalIgnoreCase)),
                roles.Any(r => string.Equals(r, "Caregiver", StringComparison.OrdinalIgnoreCase)));
        }
    }

    private static async Task<long?> GetRedisStartRankAsync(IDatabase redisDb, string feedKey, int? cursor)
    {
        if (!cursor.HasValue) return 0;
        var rank = await redisDb.SortedSetRankAsync(feedKey, cursor.Value.ToString(), Order.Descending);
        return rank.HasValue ? rank.Value + 1 : null;
    }

    private async Task AttachReactionCountsAsync(List<PostDTO> posts, CancellationToken cancellationToken)
    {
        if (posts.Count == 0) return;

        var tasks = posts.Select(async post =>
        {
            var cacheKey = $"post:{post.Id}:reactions";

            var hashEntries = await _cacheService.GetHashAllFieldsAsync(cacheKey, cancellationToken);

            post.ReactionCounts = hashEntries ?? new Dictionary<string, long>();
        });

        await Task.WhenAll(tasks);
    }

    private static string BuildFeedKey(string userId) => $"{FeedKeyPrefix}{userId}";
}