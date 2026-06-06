using FluentValidation;

namespace BreastCancer.Community.Features.UnfollowUser;

public sealed class UnfollowUserCommandValidator : AbstractValidator<UnfollowUserCommand>
{
    public UnfollowUserCommandValidator()
    {
        RuleFor(command => command.FollowerId)
            .NotEmpty()
            .WithMessage("FollowerId is required.");

        RuleFor(command => command.FollowingId)
            .NotEmpty()
            .WithMessage("FolloweeId is required.")
            .NotEqual(command => command.FollowerId)
            .WithMessage("FollowerId and FolloweeId cannot be the same.");
    }
}
