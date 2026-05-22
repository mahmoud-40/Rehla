using MediatR;

namespace BreastCancer.Community.Features;

public sealed record UnfollowUserCommand(string FollowerId, string FollowingId) : IRequest<Unit>;
