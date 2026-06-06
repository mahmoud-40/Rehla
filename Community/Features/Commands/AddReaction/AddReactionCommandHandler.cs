using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Services.Interface;
using BreastCancer.Enum;
using BreastCancer.Repository.Interface;
using MediatR;

namespace BreastCancer.Community.Features.Commands.AddReaction
{
    public class AddReactionCommandHandler : IRequestHandler<AddReactionCommand, Unit>
    {
        private readonly ILogger<AddReactionCommandHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public AddReactionCommandHandler(
            ILogger<AddReactionCommandHandler> logger,
            IUnitOfWork unitOfWork,
            ICacheService cacheService)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task<Unit> Handle(AddReactionCommand request, CancellationToken cancellationToken)
        {
            var post = await _unitOfWork.PostRepository.GetByIdAsync(request.PostId);

            if (post == null || post.IsDeleted)
            {
                throw new PostNotFoundException("Post not found.");
            }

            if (!UserCanSeePost(post.Visibility, request.Roles))
            {
                throw new PostAccessForbiddenException("You do not have permission to view or react to this post.");
            }

            var existingReactions = await _unitOfWork.ReactionRepository
                .FilterAsync(r => r.PostId == request.PostId && r.UserId == request.UserId);

            if (existingReactions.Any())
            {
                throw new DuplicateReactionException("User has already reacted to this post.");
            }

            var newReaction = new Models.Reaction
            {
                PostId = request.PostId,
                UserId = request.UserId,
                Type = request.Type
            };

            await _unitOfWork.ReactionRepository.AddAsync(newReaction);
            await _unitOfWork.SaveAsync();

            await _cacheService.IncrementHashFieldAsync(
                $"post:{request.PostId}:reactions",
                request.Type.ToString(),
                1,
                cancellationToken);

            return Unit.Value;
        }

        private bool UserCanSeePost(PostVisibility visibility, IEnumerable<string> userRoles)
        {
            var isDoctor = userRoles.Any(r => r.Equals("Doctor", StringComparison.OrdinalIgnoreCase));
            var isPatient = userRoles.Any(r => r.Equals("Patient", StringComparison.OrdinalIgnoreCase));
            var isCaregiver = userRoles.Any(r => r.Equals("Caregiver", StringComparison.OrdinalIgnoreCase));

            return visibility switch
            {
                PostVisibility.Public => true,
                PostVisibility.DoctorOnly => isDoctor,
                PostVisibility.PatientsOnly => isPatient || isDoctor,
                PostVisibility.CaregiverOnly => isCaregiver || isDoctor,
                _ => false
            };
        }
    }
}