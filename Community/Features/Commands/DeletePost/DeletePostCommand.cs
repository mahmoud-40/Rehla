using MediatR;

namespace BreastCancer.Community.Features.DeletePost;

public sealed record DeletePostCommand(int PostId, string RequesterId, IReadOnlyCollection<string> Roles) : IRequest<Unit>;
