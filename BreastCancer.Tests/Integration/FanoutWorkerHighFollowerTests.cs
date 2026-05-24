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
using System.Linq;
using System.Threading.Channels;
using Xunit;

namespace BreastCancer.Tests.Integration;

public sealed class FanoutWorkerHighFollowerTests
{
    [Fact]
    public async Task Worker_With600Followers_DoesNotPushToRedis_WritesHighFollowerPost()
    {
        // Arrange
        var services = new ServiceCollection();
        var dbRoot = new Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot();
        var dbName = $"fanout-worker-{Guid.NewGuid()}";
        services.AddDbContext<BreastCancerDB>(options => options.UseInMemoryDatabase(dbName, dbRoot));

        // Configure community options (threshold default 500)
        // Set the configured threshold via configuration string
        var dict = new Dictionary<string, string?>
        {
            { "Community:FanoutPushThreshold", "500" }
        };

        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
        configuration.Add(new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource { InitialData = dict });
        var builtConfig = configuration.Build();

        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(builtConfig);
        services.Configure<BreastCancer.Community.Options.CommunityOptions>(builtConfig.GetSection("Community"));

        await using var provider = services.BuildServiceProvider();

        // Seed 600 followers
        await using (var seedScope = provider.CreateAsyncScope())
        {
            var dbContext = seedScope.ServiceProvider.GetRequiredService<BreastCancerDB>();
            for (int i = 0; i < 600; i++)
            {
                dbContext.Follows.Add(new Follow { FollowerId = $"follower-{i}", FollowingId = "author-1" });
            }
            await dbContext.SaveChangesAsync();
        }

        var redisDbMock = new Mock<IDatabase>();
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
            PostId: 99,
            AuthorId: "author-1",
            Visibility: PostVisibility.Public,
            Timestamp: timestamp));
        channel.Writer.Complete();

        var worker = new FanoutWorker(
            channel,
            multiplexerMock.Object,
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<FanoutWorker>.Instance);

        // Act
        await worker.StartAsync(CancellationToken.None);

        // Give some time for processing
        await Task.Delay(500);

        await worker.StopAsync(CancellationToken.None);

        // Assert: ensure we didn't call Redis add for any follower
        redisDbMock.Verify(db => db.CreateBatch(It.IsAny<object?>()), Times.Never);

        // Assert: HighFollowerPost recorded
        await using (var verifyScope = provider.CreateAsyncScope())
        {
            var dbContext = verifyScope.ServiceProvider.GetRequiredService<BreastCancerDB>();
            var high = await dbContext.Set<HighFollowerPost>().FirstOrDefaultAsync(h => h.PostId == 99);
            high.Should().NotBeNull();
            high!.AuthorId.Should().Be("author-1");
        }
    }
}
