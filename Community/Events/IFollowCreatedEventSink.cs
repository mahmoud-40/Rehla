namespace BreastCancer.Community.Events;

public interface IFollowCreatedEventSink
{
    Task RecordAsync(FollowCreatedEvent domainEvent, CancellationToken cancellationToken);
}

public sealed class NoOpFollowCreatedEventSink : IFollowCreatedEventSink
{
    public Task RecordAsync(FollowCreatedEvent domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
}
