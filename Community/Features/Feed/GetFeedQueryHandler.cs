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
        var redisDb = _connectionMultiplexer.GetDatabase();
        var feedKey = BuildFeedKey(request.UserId);

        if (await redisDb.KeyExistsAsync(feedKey))
        {
            _logger.LogInformation("Feed cache hit for user {UserId}; retrieving feed from Redis.", request.UserId);
            return await GetRedisFeedAsync(request, roleFlags, normalizedLimit, redisDb, feedKey, cancellationToken);
        }

        _logger.LogInformation("Feed cache miss for user {UserId}; falling back to SQL feed query.", request.UserId);
        return await GetSqlFeedAsync(request, roleFlags, normalizedLimit, cancellationToken);
    }

    private async Task<FeedResponseDto> GetRedisFeedAsync(
        GetFeedQuery request,
        RoleFlags roleFlags,
        int normalizedLimit,
        IDatabase redisDb,
        string feedKey,
        CancellationToken cancellationToken)
    {
        var range = await GetRedisRangeAsync(redisDb, feedKey, request.Cursor, normalizedLimit + 1);
        var idsOrdered = range.Select(v => (int)v).ToList();

        if (idsOrdered.Count == 0)
        {
            return new FeedResponseDto();
        }

        var posts = await _dbContext.Posts.AsNoTracking()
            .Where(p => idsOrdered.Contains(p.Id) && !p.IsDeleted)
            .Select(p => new { p.Id, p.Visibility })
            .ToListAsync(cancellationToken);

        var postsById = posts.ToDictionary(p => p.Id);
        var visibleOrdered = new List<int>();
        foreach (var id in idsOrdered)
        {
            if (!postsById.TryGetValue(id, out var p)) continue;
            if (IsVisible(p.Visibility, roleFlags))
            {
                visibleOrdered.Add(id);
            }
        }

        return BuildResponse(visibleOrdered, normalizedLimit);
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
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(normalizedLimit + 1)
            .Select(p => new { p.Id, p.Visibility })
            .ToListAsync(cancellationToken);

        var visibleOrdered = rawPosts
            .Where(p => IsVisible(p.Visibility, roleFlags))
            .Select(p => p.Id)
            .ToList();

        return BuildResponse(visibleOrdered, normalizedLimit);
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

    private static bool IsVisible(PostVisibility visibility, RoleFlags roles)
    {
        return visibility switch
        {
            PostVisibility.Public => true,
            PostVisibility.PatientsOnly => roles.IsPatient || roles.IsDoctor,
            PostVisibility.DoctorOnly => roles.IsDoctor,
            PostVisibility.CaregiverOnly => roles.IsCaregiver || roles.IsDoctor,
            _ => false
        };
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

    private static async Task<RedisValue[]> GetRedisRangeAsync(IDatabase redisDb, string feedKey, int? cursor, int limit)
    {
        if (!cursor.HasValue)
        {
            return await redisDb.SortedSetRangeByRankAsync(feedKey, 0, limit - 1, Order.Descending);
        }

        var rank = await redisDb.SortedSetRankAsync(feedKey, cursor.Value.ToString(), Order.Descending);
        if (!rank.HasValue)
        {
            return await redisDb.SortedSetRangeByRankAsync(feedKey, 0, limit - 1, Order.Descending);
        }

        var start = rank.Value + 1;
        var stop = start + limit - 1;
        return await redisDb.SortedSetRangeByRankAsync(feedKey, start, stop, Order.Descending);
    }

    private static string BuildFeedKey(string userId) => $"{FeedKeyPrefix}{userId}";
}
