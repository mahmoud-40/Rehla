using BreastCancer.Community.Events.Models;

namespace BreastCancer.Community.Events.Sinks;

public interface IPostCreatedEventSink
{
    Task RecordAsync(PostCreatedEvent domainEvent, CancellationToken cancellationToken);
}

public sealed class NoOpPostCreatedEventSink : IPostCreatedEventSink
{
    public Task RecordAsync(PostCreatedEvent domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
}
