using BreastCancer.Community;
using BreastCancer.Community.Events;
using BreastCancer.Community.Features;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BreastCancer.Tests.Unit;

public sealed class CommunityPostCreatedEventTests
{
    [Fact]
    public async Task PostCreatedEvent_FiresAndHandledBySink()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCommunityModule();

        var recorder = new InMemoryPostCreatedEventSink();
        services.AddScoped<IPostCreatedEventSink>(_ => recorder);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new CreatePostCommand(42, "author-1"));

        recorder.Events.Should().ContainSingle();
        recorder.Events[0].PostId.Should().Be(42);
        recorder.Events[0].AuthorId.Should().Be("author-1");
    }

    private sealed class InMemoryPostCreatedEventSink : IPostCreatedEventSink
    {
        public List<PostCreatedEvent> Events { get; } = new();

        public Task RecordAsync(PostCreatedEvent domainEvent, CancellationToken cancellationToken)
        {
            Events.Add(domainEvent);
            return Task.CompletedTask;
        }
    }
}
