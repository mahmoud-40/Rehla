using MediatR;

namespace BreastCancer.Community.Features
{
    public sealed record FollowUserCommand(string FollowerId, string FollowingId) : IRequest<Unit>;
}
