using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Features.Feed;
using BreastCancer.Context;
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
    [Fact]
    public async Task Handle_WhenRedisHasFeed_ReturnsPostIdsAndNextCursor()
    {
        var redisDbMock = new Mock<IDatabase>();
        redisDbMock
            .Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        redisDbMock
            .Setup(db => db.SortedSetRangeByRankAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                Order.Descending,
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue[] { "6", "5", "4", "3" });

        var multiplexerMock = new Mock<IConnectionMultiplexer>();
        multiplexerMock
            .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
            .Returns(redisDbMock.Object);

        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase($"feed-test-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new BreastCancerDB(options);
        var loggerMock = new Mock<ILogger<GetFeedQueryHandler>>();

        var handler = new GetFeedQueryHandler(multiplexerMock.Object, dbContext, loggerMock.Object);

        var result = await handler.Handle(new GetFeedQuery("user-1", null, 3), CancellationToken.None);

        result.PostIds.Should().Equal(new[] { 6, 5, 4 });
        result.NextCursor.Should().Be(4);
    }

    [Fact]
    public async Task Handle_WhenRedisMissing_FallsBackToSqlAndLogs()
    {
        var redisDbMock = new Mock<IDatabase>();
        redisDbMock
            .Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var multiplexerMock = new Mock<IConnectionMultiplexer>();
        multiplexerMock
            .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
            .Returns(redisDbMock.Object);

        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase($"feed-test-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new BreastCancerDB(options);
        var loggerMock = new Mock<ILogger<GetFeedQueryHandler>>();

        var now = DateTime.UtcNow;
        dbContext.Follows.AddRange(
            new Follow { FollowerId = "user-1", FollowingId = "author-1" },
            new Follow { FollowerId = "user-1", FollowingId = "author-2" });

        dbContext.Posts.AddRange(
            new Post { Id = 10, AuthorId = "author-1", Content = "p1", CreatedAt = now },
            new Post { Id = 9, AuthorId = "author-2", Content = "p2", CreatedAt = now.AddMinutes(-1) },
            new Post { Id = 8, AuthorId = "author-1", Content = "p3", CreatedAt = now.AddMinutes(-2) });

        await dbContext.SaveChangesAsync();

        var handler = new GetFeedQueryHandler(multiplexerMock.Object, dbContext, loggerMock.Object);

        var result = await handler.Handle(new GetFeedQuery("user-1", null, 2), CancellationToken.None);

        result.PostIds.Should().Equal(new[] { 10, 9 });
        result.NextCursor.Should().Be(9);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Feed cache miss")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRedisHasCursor_StartsAfterCursor()
    {
        var redisDbMock = new Mock<IDatabase>();
        redisDbMock
            .Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        redisDbMock
            .Setup(db => db.SortedSetRankAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), Order.Descending, It.IsAny<CommandFlags>()))
            .ReturnsAsync(0L);

        redisDbMock
            .Setup(db => db.SortedSetRangeByRankAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                Order.Descending,
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue[] { "2", "1" });

        var multiplexerMock = new Mock<IConnectionMultiplexer>();
        multiplexerMock
            .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
            .Returns(redisDbMock.Object);

        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase($"feed-test-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new BreastCancerDB(options);
        var loggerMock = new Mock<ILogger<GetFeedQueryHandler>>();

        var handler = new GetFeedQueryHandler(multiplexerMock.Object, dbContext, loggerMock.Object);

        var result = await handler.Handle(new GetFeedQuery("user-1", 3, 2), CancellationToken.None);

        result.PostIds.Should().Equal(new[] { 2, 1 });
        result.NextCursor.Should().BeNull();
        redisDbMock.Verify(db => db.SortedSetRankAsync(It.IsAny<RedisKey>(), "3", Order.Descending, It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRedisHasMoreThanLimit_ReturnsNextCursor()
    {
        var redisDbMock = new Mock<IDatabase>();
        redisDbMock
            .Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        redisDbMock
            .Setup(db => db.SortedSetRangeByRankAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                Order.Descending,
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue[] { "5", "4", "3" });

        var multiplexerMock = new Mock<IConnectionMultiplexer>();
        multiplexerMock
            .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
            .Returns(redisDbMock.Object);

        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase($"feed-test-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new BreastCancerDB(options);
        var loggerMock = new Mock<ILogger<GetFeedQueryHandler>>();

        var handler = new GetFeedQueryHandler(multiplexerMock.Object, dbContext, loggerMock.Object);

        var result = await handler.Handle(new GetFeedQuery("user-1", null, 2), CancellationToken.None);

        result.PostIds.Should().Equal(new[] { 5, 4 });
        result.NextCursor.Should().Be(4);
    }

    [Fact]
    public async Task Handle_WhenSqlHasMoreThanLimit_ReturnsNextCursor()
    {
        var redisDbMock = new Mock<IDatabase>();
        redisDbMock
            .Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var multiplexerMock = new Mock<IConnectionMultiplexer>();
        multiplexerMock
            .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
            .Returns(redisDbMock.Object);

        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase($"feed-test-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new BreastCancerDB(options);
        var loggerMock = new Mock<ILogger<GetFeedQueryHandler>>();

        var now = DateTime.UtcNow;
        dbContext.Follows.Add(new Follow { FollowerId = "user-1", FollowingId = "author-1" });

        dbContext.Posts.AddRange(
            new Post { Id = 10, AuthorId = "author-1", Content = "p1", CreatedAt = now },
            new Post { Id = 9, AuthorId = "author-1", Content = "p2", CreatedAt = now.AddMinutes(-1) },
            new Post { Id = 8, AuthorId = "author-1", Content = "p3", CreatedAt = now.AddMinutes(-2) });

        await dbContext.SaveChangesAsync();

        var handler = new GetFeedQueryHandler(multiplexerMock.Object, dbContext, loggerMock.Object);

        var result = await handler.Handle(new GetFeedQuery("user-1", null, 2), CancellationToken.None);

        result.PostIds.Should().Equal(new[] { 10, 9 });
        result.NextCursor.Should().Be(9);
    }
}
