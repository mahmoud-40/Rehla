namespace BreastCancer.Community.Events;

public interface IPostCreatedEventSink
{
    Task RecordAsync(PostCreatedEvent domainEvent, CancellationToken cancellationToken);
}

public sealed class NoOpPostCreatedEventSink : IPostCreatedEventSink
{
    public Task RecordAsync(PostCreatedEvent domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
}
