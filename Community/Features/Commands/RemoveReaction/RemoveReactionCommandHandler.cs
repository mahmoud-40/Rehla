using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Services.Interface;
using BreastCancer.Enum;
using BreastCancer.Repository.Interface;
using MediatR;

namespace BreastCancer.Community.Features.Commands.RemoveReaction
{
    public class RemoveReactionCommandHandler : IRequestHandler<RemoveReactionCommand, Unit>
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<RemoveReactionCommandHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveReactionCommandHandler(
            ICacheService cacheService,
            ILogger<RemoveReactionCommandHandler> logger,
            IUnitOfWork unitOfWork)
        {
            _cacheService = cacheService;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }
        public async Task<Unit> Handle(RemoveReactionCommand request, CancellationToken cancellationToken)
        {
            var reaction = await _unitOfWork.ReactionRepository.GetReactionByPostIdAndUserIdAsync(request.PostId, request.UserId);
            if (reaction == null)
            {
                throw new ReactionNotFoundException("You have not reacted to this post.");
            }

            ReactionType type = reaction.Type;

            _unitOfWork.ReactionRepository.Delete(reaction);

            await _unitOfWork.SaveAsync();

            await _cacheService.DecrementHashFieldAsync(
                $"post:{request.PostId}:reactions", 
                type.ToString(), 
                1, 
                cancellationToken);

            return Unit.Value;
        }
    }
}
