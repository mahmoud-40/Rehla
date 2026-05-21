using MediatR;

namespace BreastCancer.Community.Features;

public sealed record CreatePostCommand(int PostId, string AuthorId) : IRequest<Unit>;
