using BreastCancer.Community;
using BreastCancer.Community.Events;
using BreastCancer.Community.Events.Models;
using BreastCancer.Community.Workers.Fanout;
using BreastCancer.Enum;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Channels;
using Xunit;

namespace BreastCancer.Tests.Integration;

public sealed class FanoutChannelIntegrationTests
{
    [Fact]
    public async Task PublishPostCreatedEvent_EnqueuesFanoutJobInChannel()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Redis:ConnectionString", "localhost:6379" },
                { "Redis:DefaultTTLSeconds", "3600" }
            })
            .Build();

        services.AddCommunityModule(configuration);

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var channel = provider.GetRequiredService<Channel<FanoutJob>>();

        var now = DateTimeOffset.UtcNow;
        await mediator.Publish(new PostCreatedEvent(42, "author-1", PostVisibility.Public));

        var wasRead = channel.Reader.TryRead(out var job);

        wasRead.Should().BeTrue();
        job.Should().NotBeNull();
        job!.PostId.Should().Be(42);
        job.AuthorId.Should().Be("author-1");
        job.Visibility.Should().Be(PostVisibility.Public);
        job.Timestamp.Should().BeOnOrAfter(now);
    }
}
