using BreastCancer.Community.Events.Models;

namespace BreastCancer.Community.Events.Sinks;

public interface IFollowCreatedEventSink
{
    Task RecordAsync(FollowCreatedEvent domainEvent, CancellationToken cancellationToken);
}

public sealed class NoOpFollowCreatedEventSink : IFollowCreatedEventSink
{
    public Task RecordAsync(FollowCreatedEvent domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
}
