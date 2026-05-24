using BreastCancer.Community.Events;
using BreastCancer.Community.Events.Models;
using BreastCancer.Community.Exceptions;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using MediatR;

namespace BreastCancer.Community.Features.FollowUser
{
    public sealed class FollowUserCommandHandler : IRequestHandler<FollowUserCommand, Unit>
    {
        private readonly IPublisher _publisher;
        private readonly IUnitOfWork _unitOfWork;

        public FollowUserCommandHandler(IPublisher publisher, IUnitOfWork unitOfWork)
        {
            _publisher = publisher;
            _unitOfWork = unitOfWork;
        }


        public async Task<Unit> Handle(FollowUserCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            var committed = false;
            try
            {
                // TODO: validate that both users exist
                var existing = await _unitOfWork.FollowRepository.FilterAsync(follow =>
                    follow.FollowerId == request.FollowerId && follow.FollowingId == request.FollowingId);

                if (existing.Any())
                {
                    throw new AlreadyFollowingException("You are already following this user.");
                }

                Follow follow = new Follow
                {
                    FollowerId = request.FollowerId,
                    FollowingId = request.FollowingId
                };

                await _unitOfWork.FollowRepository.AddAsync(follow);
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

            await _publisher.Publish(new FollowCreatedEvent(request.FollowerId, request.FollowingId), cancellationToken);

            return Unit.Value;
        }
    }
}
