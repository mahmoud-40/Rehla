using BreastCancer.Community.Events;
using MediatR;

namespace BreastCancer.Community.Features;

public sealed class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, int>
{
    private readonly IPublisher _publisher;

    public CreatePostCommandHandler(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task<int> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        const int postId = 0; // TODO: Implement actual post creation logic and generate a real post ID
        await _publisher.Publish(new PostCreatedEvent(postId, request.AuthorId), cancellationToken);
        return postId;
    }
}
