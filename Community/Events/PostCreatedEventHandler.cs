using MediatR;

namespace BreastCancer.Community.Events;

public sealed class PostCreatedEventHandler : INotificationHandler<PostCreatedEvent>
{
    private readonly IPostCreatedEventSink _sink;

    public PostCreatedEventHandler(IPostCreatedEventSink sink)
    {
        _sink = sink;
    }

    public Task Handle(PostCreatedEvent notification, CancellationToken cancellationToken)
        => _sink.RecordAsync(notification, cancellationToken);
}
