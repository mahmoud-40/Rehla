using BreastCancer.Community.Events.Models;
using BreastCancer.Community.Events.Sinks;
using MediatR;

namespace BreastCancer.Community.Events.Handlers;

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
