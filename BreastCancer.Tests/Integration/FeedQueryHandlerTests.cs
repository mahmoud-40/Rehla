using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Features.Feed;
using BreastCancer.Community.Services.Interface;
using BreastCancer.Context;
using BreastCancer.Enum;
using BreastCancer.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace BreastCancer.Tests.Integration;

public sealed class FeedQueryHandlerTests
{
    private readonly Mock<IDatabase> _redisDbMock = new();
    private readonly Mock<IConnectionMultiplexer> _multiplexerMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<ILogger<GetFeedQueryHandler>> _loggerMock = new();
    private readonly DbContextOptions<BreastCancerDB> _dbOptions;

    public FeedQueryHandlerTests()
    {
        _multiplexerMock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(_redisDbMock.Object);
        _dbOptions = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase($"feed-test-{Guid.NewGuid()}")
            .Options;
    }

    [Fact]
    public async Task Handle_WhenRedisHasFeed_ReturnsPostIdsAndNextCursor()
    {
        _redisDbMock.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        _redisDbMock.Setup(db => db.SortedSetRangeByRankAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<long>(), Order.Descending, It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue[] { "6", "5", "4", "3" });

        await using var dbContext = new BreastCancerDB(_dbOptions);
        dbContext.Posts.AddRange(
            new Post { Id = 6, AuthorId = "author-1", Content = "Test post content", Visibility = PostVisibility.Public, CreatedAt = DateTime.UtcNow },
            new Post { Id = 5, AuthorId = "author-1", Content = "Test post content", Visibility = PostVisibility.Public, CreatedAt = DateTime.UtcNow.AddMinutes(-1) },
            new Post { Id = 4, AuthorId = "author-1", Content = "Test post content", Visibility = PostVisibility.Public, CreatedAt = DateTime.UtcNow.AddMinutes(-2) },
            new Post { Id = 3, AuthorId = "author-1", Content = "Test post content", Visibility = PostVisibility.Public, CreatedAt = DateTime.UtcNow.AddMinutes(-3) });
        await dbContext.SaveChangesAsync();

        var handler = new GetFeedQueryHandler(_multiplexerMock.Object, dbContext, _cacheServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetFeedQuery("user-1", null, 3), CancellationToken.None);

        result.Posts.Select(p => p.Id).Should().Equal(new[] { 6, 5, 4 });
        result.NextCursor.Should().Be(4);
    }

    [Fact]
    public async Task Handle_WhenRedisMissing_FallsBackToSqlAndLogs()
    {
        _redisDbMock.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(false);

        await using var dbContext = new BreastCancerDB(_dbOptions);
        var now = DateTime.UtcNow;
        dbContext.Follows.AddRange(
            new Follow { FollowerId = "user-1", FollowingId = "author-1" },
            new Follow { FollowerId = "user-1", FollowingId = "author-2" });

        dbContext.Posts.AddRange(
            new Post { Id = 10, AuthorId = "author-1", Content = "Test post content", Visibility = PostVisibility.Public, CreatedAt = now },
            new Post { Id = 9, AuthorId = "author-2", Content = "Test post content", Visibility = PostVisibility.Public, CreatedAt = now.AddMinutes(-1) },
            new Post { Id = 8, AuthorId = "author-1", Content = "Test post content", Visibility = PostVisibility.Public, CreatedAt = now.AddMinutes(-2) });
        await dbContext.SaveChangesAsync();

        var handler = new GetFeedQueryHandler(_multiplexerMock.Object, dbContext, _cacheServiceMock.Object, _loggerMock.Object);

        var result = await handler.Handle(new GetFeedQuery("user-1", null, 2), CancellationToken.None);

        result.Posts.Select(p => p.Id).Should().Equal(new[] { 10, 9 });
        result.NextCursor.Should().Be(9);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Feed cache miss")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Hydration_With15CacheHitsAnd5Misses_BatchesSqlAndSetsCache()
    {
        // Arrange
        _redisDbMock.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);

        var feedIds = Enumerable.Range(1, 20).Select(id => (RedisValue)id.ToString()).ToArray();
        _redisDbMock.Setup(db => db.SortedSetRangeByRankAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<long>(), Order.Descending, It.IsAny<CommandFlags>()))
            .ReturnsAsync(feedIds);

        // Setup Cache Hits for 1-15, Misses for 16-20
        _cacheServiceMock.Setup(c => c.GetAsync<PostDTO>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken _) =>
            {
                var idStr = key.Split(':').Last();
                if (int.TryParse(idStr, out int id) && id <= 15)
                {
                    return new PostDTO { Id = id, Content = "Cached", PostVisibility = PostVisibility.Public, AuthorId = "author-1" };
                }
                return null;
            });

        var stringSetCallCount = 0;
        _cacheServiceMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<PostDTO>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Callback(() => stringSetCallCount++) // Callback BEFORE Returns/ReturnsAsync guarantees execution
            .Returns(Task.CompletedTask);

        await using var dbContext = new BreastCancerDB(_dbOptions);
        for (int i = 1; i <= 20; i++)
        {
            dbContext.Posts.Add(new Post
            {
                Id = i,
                AuthorId = "author-1",
                Content = i <= 15 ? "Cached" : "FromDB",
                Visibility = PostVisibility.Public,
                IsDeleted = false
            });
        }
        await dbContext.SaveChangesAsync();

        var handler = new GetFeedQueryHandler(_multiplexerMock.Object, dbContext, _cacheServiceMock.Object, _loggerMock.Object);

        // Act
        var result = await handler.Handle(new GetFeedQuery("user-1", null, 20), CancellationToken.None);

        // Assert
        result.Posts.Should().HaveCount(20);
        result.Posts.Count(p => p.Content == "Cached").Should().Be(15);
        result.Posts.Count(p => p.Content == "FromDB").Should().Be(5);

        stringSetCallCount.Should().Be(5);
    }
}