using BreastCancer.Community.Events.Models;
using BreastCancer.Community.Events.Sinks;

namespace BreastCancer.Community.Events.Handlers;

public class CommentCreatedEventHandler
{
    private readonly ICommentCreatedEventSink _sink;
    public CommentCreatedEventHandler(ICommentCreatedEventSink sink)
    {
        _sink = sink;
    }
    public Task Handle(CommentCreatedEvent notification, CancellationToken cancellationToken)
        => _sink.RecordAsync(notification, cancellationToken);
}