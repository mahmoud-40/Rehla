using BreastCancer.Community.Events;
using MediatR;

namespace BreastCancer.Community.Features;

public sealed class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, Unit>
{
    private readonly IPublisher _publisher;

    public CreatePostCommandHandler(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task<Unit> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        await _publisher.Publish(new PostCreatedEvent(request.PostId, request.AuthorId), cancellationToken);
        return Unit.Value;
    }
}
