using BreastCancer.Community.DTO.response;
using BreastCancer.Context;
using BreastCancer.Enum;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace BreastCancer.Community.Features.Feed;

public sealed class GetFeedQueryHandler : IRequestHandler<GetFeedQuery, FeedResponseDto>
{
    private const string FeedKeyPrefix = "rehla:community:feed:";
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly BreastCancerDB _dbContext;
    private readonly ILogger<GetFeedQueryHandler> _logger;

    public GetFeedQueryHandler(IConnectionMultiplexer connectionMultiplexer, BreastCancerDB dbContext, ILogger<GetFeedQueryHandler> logger)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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

        if (await redisDb.KeyExistsAsync(feedKey))
        {
            _logger.LogInformation("Feed cache hit for user {UserId}; retrieving feed from Redis.", request.UserId);
            return await GetRedisFeedAsync(request, allowedVisibilities, normalizedLimit, redisDb, feedKey, cancellationToken);
        }

        _logger.LogInformation("Feed cache miss for user {UserId}; falling back to SQL feed query.", request.UserId);
        return await GetSqlFeedAsync(request, roleFlags, normalizedLimit, cancellationToken);
    }

    private async Task<FeedResponseDto> GetRedisFeedAsync(
        GetFeedQuery request,
        IReadOnlySet<PostVisibility> allowedVisibilities,
        int normalizedLimit,
        IDatabase redisDb,
        string feedKey,
        CancellationToken cancellationToken)
    {
        var batchSize = normalizedLimit + 1;
        var visibleOrdered = new List<int>();
        int? scannedLastId = null;
        var start = await GetRedisStartRankAsync(redisDb, feedKey, request.Cursor);
        var exhausted = false;

        while (visibleOrdered.Count < normalizedLimit + 1)
        {
            var batch = await redisDb.SortedSetRangeByRankAsync(feedKey, start, start + batchSize - 1, Order.Descending);
            if (batch.Length == 0)
            {
                exhausted = true;
                break;
            }

            var idsOrdered = batch.Select(v => (int)v).ToList();
            scannedLastId = idsOrdered[^1];

            var posts = await _dbContext.Posts.AsNoTracking()
                .Where(p => idsOrdered.Contains(p.Id) && !p.IsDeleted)
                .Select(p => new { p.Id, p.Visibility })
                .ToListAsync(cancellationToken);

            var postsById = posts.ToDictionary(p => p.Id);
            foreach (var id in idsOrdered)
            {
                if (visibleOrdered.Count >= normalizedLimit + 1)
                {
                    break;
                }

                if (!postsById.TryGetValue(id, out var p))
                {
                    continue;
                }

                if (allowedVisibilities.Contains(p.Visibility))
                {
                    visibleOrdered.Add(id);
                }
            }

            if (batch.Length < batchSize)
            {
                exhausted = true;
                break;
            }

            start += batch.Length;
        }

        var response = BuildResponse(visibleOrdered, normalizedLimit);
        if (response.PostIds.Count == 0 && !exhausted && scannedLastId.HasValue)
        {
            response = new FeedResponseDto
            {
                PostIds = response.PostIds,
                NextCursor = scannedLastId.Value
            };
        }

        return response;
    }

    private async Task<FeedResponseDto> GetSqlFeedAsync(
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

        var rawPosts = await query
            .Where(p => p.Visibility == PostVisibility.Public
                || (roleFlags.IsDoctor && (p.Visibility == PostVisibility.DoctorOnly
                    || p.Visibility == PostVisibility.PatientsOnly
                    || p.Visibility == PostVisibility.CaregiverOnly))
                || (!roleFlags.IsDoctor && roleFlags.IsPatient && p.Visibility == PostVisibility.PatientsOnly)
                || (!roleFlags.IsDoctor && roleFlags.IsCaregiver && p.Visibility == PostVisibility.CaregiverOnly))
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(normalizedLimit + 1)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        return BuildResponse(rawPosts, normalizedLimit);
    }

    private static FeedResponseDto BuildResponse(List<int> visibleOrdered, int normalizedLimit)
    {
        var visiblePage = visibleOrdered.Take(normalizedLimit).ToList();
        var hasVisibleMore = visibleOrdered.Count > normalizedLimit;

        return new FeedResponseDto
        {
            PostIds = visiblePage,
            NextCursor = hasVisibleMore && visiblePage.Count > 0 ? visiblePage[^1] : null
        };
    }

    private static IReadOnlySet<PostVisibility> GetAllowedVisibilities(RoleFlags roles)
    {
        var allowed = new HashSet<PostVisibility> { PostVisibility.Public };

        if (roles.IsDoctor)
        {
            allowed.Add(PostVisibility.DoctorOnly);
            allowed.Add(PostVisibility.PatientsOnly);
            allowed.Add(PostVisibility.CaregiverOnly);
            return allowed;
        }

        if (roles.IsPatient)
        {
            allowed.Add(PostVisibility.PatientsOnly);
        }

        if (roles.IsCaregiver)
        {
            allowed.Add(PostVisibility.CaregiverOnly);
        }

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

    private static async Task<long> GetRedisStartRankAsync(IDatabase redisDb, string feedKey, int? cursor)
    {
        if (!cursor.HasValue)
        {
            return 0;
        }

        var rank = await redisDb.SortedSetRankAsync(feedKey, cursor.Value.ToString(), Order.Descending);
        if (!rank.HasValue)
        {
            return 0;
        }

        return rank.Value + 1;
    }

    private static string BuildFeedKey(string userId) => $"{FeedKeyPrefix}{userId}";
}
