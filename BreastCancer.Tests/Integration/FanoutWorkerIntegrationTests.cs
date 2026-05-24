using BreastCancer.Community.Events;
using BreastCancer.Community.Workers.Fanout;
using BreastCancer.Context;
using BreastCancer.Enum;
using BreastCancer.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;
using System.Threading.Channels;
using Xunit;

namespace BreastCancer.Tests.Integration;

public sealed class FanoutWorkerIntegrationTests
{
    [Fact]
    public async Task Worker_ConsumesJob_AndWritesPostIdToFollowerFeedsUsingSortedSetBatch()
    {
        // Arrange
        var services = new ServiceCollection();
        var dbRoot = new Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot();
        var dbName = $"fanout-worker-{Guid.NewGuid()}";
        services.AddDbContext<BreastCancerDB>(options => options.UseInMemoryDatabase(dbName, dbRoot));

        await using var provider = services.BuildServiceProvider();

        await using (var seedScope = provider.CreateAsyncScope())
        {
            var dbContext = seedScope.ServiceProvider.GetRequiredService<BreastCancerDB>();
            dbContext.Follows.AddRange(
                new Follow { FollowerId = "follower-1", FollowingId = "author-1" },
                new Follow { FollowerId = "follower-2", FollowingId = "author-1" },
                new Follow { FollowerId = "other-follower", FollowingId = "someone-else" });

            await dbContext.SaveChangesAsync();
        }

        await using (var verifyScope = provider.CreateAsyncScope())
        {
            var dbContext = verifyScope.ServiceProvider.GetRequiredService<BreastCancerDB>();
            var seededFollowerCount = await dbContext.Follows.CountAsync(f => f.FollowingId == "author-1");
            seededFollowerCount.Should().Be(2);
        }

        var executeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var batchMock = new Mock<IBatch>();

        // Setup common overloads so queued batch tasks complete successfully.
        batchMock
            .Setup(b => b.SortedSetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<double>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        batchMock
            .Setup(b => b.SortedSetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<double>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        batchMock
            .Setup(b => b.SortedSetRemoveRangeByRankAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(0L);
        batchMock
            .Setup(b => b.SortedSetRemoveRangeByRankAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<long>()))
            .ReturnsAsync(0L);

        batchMock
            .Setup(b => b.Execute())
            .Callback(() => executeTcs.TrySetResult());

        var redisDbMock = new Mock<IDatabase>();
        redisDbMock
            .Setup(db => db.CreateBatch(It.IsAny<object?>()))
            .Returns(batchMock.Object);

        var multiplexerMock = new Mock<IConnectionMultiplexer>();
        multiplexerMock
            .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
            .Returns(redisDbMock.Object);

        var channel = Channel.CreateBounded<FanoutJob>(new BoundedChannelOptions(5000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

        var timestamp = DateTimeOffset.UtcNow;
        await channel.Writer.WriteAsync(new FanoutJob(
            PostId: 42,
            AuthorId: "author-1",
            Visibility: PostVisibility.Public,
            Timestamp: timestamp));
        channel.Writer.Complete();

        var worker = new FanoutWorker(
            channel,
            multiplexerMock.Object,
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<FanoutWorker>.Instance,
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<BreastCancer.Community.Options.CommunityOptions>>());

        // Act
        await worker.StartAsync(CancellationToken.None);

        var completed = await Task.WhenAny(executeTcs.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        completed.Should().Be(executeTcs.Task, "the worker should execute Redis batch for the queued fanout job");

        await worker.StopAsync(CancellationToken.None);

        // Assert
        var expectedScore = timestamp.ToUnixTimeSeconds();

        var addInvocations = batchMock.Invocations
            .Where(i => i.Method.Name == nameof(IBatch.SortedSetAddAsync))
            .ToList();

        addInvocations.Should().HaveCount(2);
        addInvocations.Select(i => ((RedisValue)i.Arguments[1]).ToString()).Should().OnlyContain(member => member == "42");
        addInvocations.Select(i => (double)i.Arguments[2]).Should().OnlyContain(score => score == expectedScore);
        addInvocations.Select(i => ((RedisKey)i.Arguments[0]).ToString()).Should().BeEquivalentTo(new[]
        {
            "rehla:community:feed:follower-1",
            "rehla:community:feed:follower-2"
        });

        var trimInvocations = batchMock.Invocations
            .Where(i => i.Method.Name == nameof(IBatch.SortedSetRemoveRangeByRankAsync))
            .ToList();

        trimInvocations.Should().HaveCount(2);
        trimInvocations.Select(i => (long)i.Arguments[1]).Should().OnlyContain(start => start == 0);
        trimInvocations.Select(i => (long)i.Arguments[2]).Should().OnlyContain(stop => stop == -501);
        trimInvocations.Select(i => ((RedisKey)i.Arguments[0]).ToString()).Should().BeEquivalentTo(new[]
        {
            "rehla:community:feed:follower-1",
            "rehla:community:feed:follower-2"
        });

        batchMock.Verify(b => b.Execute(), Times.Once);
        redisDbMock.Verify(db => db.CreateBatch(It.IsAny<object?>()), Times.Once);
        multiplexerMock.Verify(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()), Times.Once);
    }
}
