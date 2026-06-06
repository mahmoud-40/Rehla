using MediatR;

namespace BreastCancer.Community.Features.FollowUser
{
    public sealed record FollowUserCommand(string FollowerId, string FollowingId) : IRequest<Unit>;
}
