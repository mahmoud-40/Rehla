using MediatR;

namespace BreastCancer.Community.Events;

public sealed class FollowCreatedEventHandler : INotificationHandler<FollowCreatedEvent>
{
    private readonly IFollowCreatedEventSink _sink;

    public FollowCreatedEventHandler(IFollowCreatedEventSink sink)
    {
        _sink = sink;
    }

    public Task Handle(FollowCreatedEvent notification, CancellationToken cancellationToken)
        => _sink.RecordAsync(notification, cancellationToken);
}
