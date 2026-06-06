using MediatR;

namespace BreastCancer.Community.Features.UnfollowUser;

public sealed record UnfollowUserCommand(string FollowerId, string FollowingId) : IRequest<Unit>;
