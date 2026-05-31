using MediatR;

namespace BreastCancer.Community.Features.Posts;

public sealed record DeletePostCommand(int PostId, string RequesterId, IReadOnlyCollection<string> Roles) : IRequest<Unit>;
