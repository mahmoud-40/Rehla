using MediatR;

namespace BreastCancer.Community.Features;

public sealed record CreatePostCommand(string AuthorId) : IRequest<int>;
