using MediatR;
using System.Threading.Channels;
using BreastCancer.Community.Events.Models;

namespace BreastCancer.Community.Workers.Fanout;

public sealed class FanoutHandler : INotificationHandler<PostCreatedEvent>
{
    private readonly Channel<FanoutJob> _fanoutChannel;
    private readonly ILogger<FanoutHandler> _logger;

    public FanoutHandler(Channel<FanoutJob> fanoutChannel, ILogger<FanoutHandler> logger)
    {
        _fanoutChannel = fanoutChannel ?? throw new ArgumentNullException(nameof(fanoutChannel));
        _logger = logger;
    }

    public async Task Handle(PostCreatedEvent notification, CancellationToken cancellationToken)
    {
        var job = new FanoutJob(
            notification.PostId,
            notification.AuthorId,
            notification.Visibility,
            DateTimeOffset.UtcNow);

        _logger.LogInformation(
            "Enqueuing fanout job for post {PostId} by author {AuthorId} with visibility {Visibility}",
            job.PostId,
            job.AuthorId,
            job.Visibility);

        await _fanoutChannel.Writer.WriteAsync(job, cancellationToken);
    }
}
