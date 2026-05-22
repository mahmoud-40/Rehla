using AutoMapper;
using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Events;
using BreastCancer.Community.Exceptions;
using BreastCancer.Enum;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using MediatR;

namespace BreastCancer.Community.Features;

public sealed class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, PostDTO>
{
    private readonly IMapper _mapper;
    private readonly IPublisher _publisher;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePostCommandHandler(IMapper mapper, IPublisher publisher, IUnitOfWork unitOfWork)
    {
        _mapper = mapper;
        _publisher = publisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<PostDTO> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (request.Post.Type == PostType.DoctorUpdate
                && !request.Roles.Any(role => role.Equals("Doctor", StringComparison.OrdinalIgnoreCase)))
            {
                throw new PostAccessForbiddenException("Only doctors can create DoctorUpdate posts.");
            }

            var post = _mapper.Map<Models.Post>(request.Post);
            post.AuthorId = request.AuthorId;
            post.MediaUrls = request.Post.MediaUrls ?? new List<string>();

            await _unitOfWork.PostRepository.AddAsync(post);

            await _unitOfWork.SaveAsync();

            await transaction.CommitAsync();

            await _publisher.Publish(new PostCreatedEvent(post.Id, post.AuthorId, post.Visibility), cancellationToken);

            return _mapper.Map<PostDTO>(post);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
