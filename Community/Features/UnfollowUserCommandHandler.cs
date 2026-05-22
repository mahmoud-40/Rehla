using BreastCancer.Community.Exceptions;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using MediatR;

namespace BreastCancer.Community.Features;

public sealed class UnfollowUserCommandHandler : IRequestHandler<UnfollowUserCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public UnfollowUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UnfollowUserCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        var committed = false;
        try
        {
            // TODO: validate that both users exist
            var matches = await _unitOfWork.FollowRepository.FilterAsync(follow =>
                follow.FollowerId == request.FollowerId && follow.FollowingId == request.FollowingId);

            var follow = matches.FirstOrDefault();
            if (follow == null)
            {
                throw new UserNotFoundException("Follow relationship not found.");
            }

            _unitOfWork.FollowRepository.Delete(follow);
            await _unitOfWork.SaveAsync();

            await transaction.CommitAsync();
            committed = true;
        }
        catch
        {
            if (!committed)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }

        return Unit.Value;
    }
}
