using BreastCancer.Community.DTO.response;
using BreastCancer.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BreastCancer.Community.Features.Feed;

public sealed class GetFeedQueryHandler : IRequestHandler<GetFeedQuery, FeedResponseDto>
{
    private const string FeedKeyPrefix = "community:feed:";
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
        var redisDb = _connectionMultiplexer.GetDatabase();
        var feedKey = BuildFeedKey(request.UserId);

        if (await redisDb.KeyExistsAsync(feedKey))
        {
            var range = await GetRedisRangeAsync(redisDb, feedKey, request.Cursor, normalizedLimit);
            var postIds = range.Select(value => (int)value).ToList();

            return new FeedResponseDto
            {
                PostIds = postIds,
                NextCursor = postIds.Count == normalizedLimit ? postIds[^1] : null
            };
        }

        _logger.LogInformation("Feed cache miss for user {UserId}; falling back to SQL feed query.", request.UserId);

        var query = from follow in _dbContext.Follows.AsNoTracking()
                    join post in _dbContext.Posts.AsNoTracking() on follow.FollowingId equals post.AuthorId
                    where follow.FollowerId == request.UserId && !post.IsDeleted
                    select post;

        if (request.Cursor.HasValue)
        {
            query = query.Where(p => p.Id < request.Cursor.Value);
        }

        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(normalizedLimit)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        return new FeedResponseDto
        {
            PostIds = posts,
            NextCursor = posts.Count == normalizedLimit ? posts[^1] : null
        };
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
