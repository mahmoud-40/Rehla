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

public sealed class FeedVisibilityTests
{
    public static IEnumerable<object[]> VisibilityMatrix()
    {
        // role, visibility, expectedVisible
        yield return new object[] { new[] { "Patient" }, PostVisibility.Public, true };
        yield return new object[] { new[] { "Doctor" }, PostVisibility.Public, true };
        yield return new object[] { new[] { "Caregiver" }, PostVisibility.Public, true };

        yield return new object[] { new[] { "Patient" }, PostVisibility.PatientsOnly, true };
        yield return new object[] { new[] { "Doctor" }, PostVisibility.PatientsOnly, true };
        yield return new object[] { new[] { "Caregiver" }, PostVisibility.PatientsOnly, false };

        yield return new object[] { new[] { "Doctor" }, PostVisibility.DoctorOnly, true };
        yield return new object[] { new[] { "Patient" }, PostVisibility.DoctorOnly, false };
        yield return new object[] { new[] { "Caregiver" }, PostVisibility.DoctorOnly, false };

        yield return new object[] { new[] { "Caregiver" }, PostVisibility.CaregiverOnly, true };
        yield return new object[] { new[] { "Doctor" }, PostVisibility.CaregiverOnly, true };
        yield return new object[] { new[] { "Patient" }, PostVisibility.CaregiverOnly, false };

    }

    [Theory]
    [MemberData(nameof(VisibilityMatrix))]
    public async Task FeedVisibility_AppliesFiltering(string[] roles, PostVisibility visibility, bool expectedVisible)
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
            .UseInMemoryDatabase($"feed-visibility-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new BreastCancerDB(options);
        var loggerMock = new Mock<ILogger<GetFeedQueryHandler>>();

        var cacheServiceMock = new Mock<ICacheService>();

        var userId = "user-1";
        var authorId = "author-1";

        // Ensure the feed includes the author's posts via follow
        dbContext.Follows.Add(new Follow { FollowerId = userId, FollowingId = authorId });

        dbContext.Posts.Add(new Post { Id = 1, AuthorId = authorId, Content = "x", Visibility = visibility, CreatedAt = DateTime.UtcNow });

        await dbContext.SaveChangesAsync();

        var handler = new GetFeedQueryHandler(multiplexerMock.Object, dbContext, cacheServiceMock.Object, loggerMock.Object);

        var query = new GetFeedQuery(userId, null, 10, roles);
        var result = await handler.Handle(query, CancellationToken.None);

        var contains = result.Posts.Any(p => p.Id == 1);
        contains.Should().Be(expectedVisible);
    }

    [Theory]
    [MemberData(nameof(VisibilityMatrix))]
    public async Task FeedVisibility_AppliesFiltering_WhenRedisHit(string[] roles, PostVisibility visibility, bool expectedVisible)
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
            .ReturnsAsync(new RedisValue[] { "1" });

        var multiplexerMock = new Mock<IConnectionMultiplexer>();
        multiplexerMock
            .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
            .Returns(redisDbMock.Object);

        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase($"feed-visibility-redis-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new BreastCancerDB(options);
        var loggerMock = new Mock<ILogger<GetFeedQueryHandler>>();

        var cacheServiceMock = new Mock<ICacheService>();

        var userId = "user-1";
        var authorId = "author-1";

        dbContext.Posts.Add(new Post { Id = 1, AuthorId = authorId, Content = "x", Visibility = visibility, CreatedAt = DateTime.UtcNow });

        await dbContext.SaveChangesAsync();

        var handler = new GetFeedQueryHandler(multiplexerMock.Object, dbContext, cacheServiceMock.Object, loggerMock.Object);

        var query = new GetFeedQuery(userId, null, 10, roles);
        var result = await handler.Handle(query, CancellationToken.None);

        var contains = result.Posts.Any(p => p.Id == 1);
        contains.Should().Be(expectedVisible);
    }
}