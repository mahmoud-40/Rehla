using MediatR;
using System.Threading.Channels;
using BreastCancer.Community.Events.Models;

namespace BreastCancer.Community.Workers.Fanout;

public sealed class FanoutHandler : INotificationHandler<PostCreatedEvent>
{
    private readonly Channel<FanoutJob> _fanoutChannel;

    public FanoutHandler(Channel<FanoutJob> fanoutChannel)
    {
        _fanoutChannel = fanoutChannel ?? throw new ArgumentNullException(nameof(fanoutChannel));
    }

    public async Task Handle(PostCreatedEvent notification, CancellationToken cancellationToken)
    {
        var job = new FanoutJob(
            notification.PostId,
            notification.AuthorId,
            notification.Visibility,
            DateTimeOffset.UtcNow);

        await _fanoutChannel.Writer.WriteAsync(job, cancellationToken);
    }
}
