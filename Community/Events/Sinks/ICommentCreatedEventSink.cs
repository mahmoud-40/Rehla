using BreastCancer.Community.Events.Models;

namespace BreastCancer.Community.Events.Sinks;

public interface ICommentCreatedEventSink
{
    Task RecordAsync(CommentCreatedEvent domainEvent, CancellationToken cancellationToken);
}

public sealed class NoOpCommentCreatedEventSink : ICommentCreatedEventSink
{
    public Task RecordAsync(CommentCreatedEvent domainEvent, CancellationToken cancellationToken) 
        => Task.CompletedTask;
}