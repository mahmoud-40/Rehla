using AutoMapper;
using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Services.Interface;
using BreastCancer.Context;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace BreastCancer.Community.Features.UpdatePost;

public sealed class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand, PostDTO>
{
    private const string PostCacheKeyPrefix = "post:";
    private readonly BreastCancerDB _dbContext;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;

    public UpdatePostCommandHandler(BreastCancerDB dbContext, IMapper mapper, ICacheService cacheService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    public async Task<PostDTO> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts.FirstOrDefaultAsync(p => p.Id == request.PostId && !p.IsDeleted, cancellationToken);
        if (post is null)
        {
            throw new PostNotFoundException("Post not found.");
        }

        if (!string.Equals(post.AuthorId, request.RequesterId, StringComparison.OrdinalIgnoreCase))
        {
            throw new PostAccessForbiddenException("Only the author can update this post.");
        }

        post.Content = request.Post.Content.Trim();
        post.Visibility = request.Post.Visibility;
        post.MediaUrls = request.Post.MediaUrls ?? new List<string>();
        post.IsEdited = true;
        post.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var updatedDto = _mapper.Map<PostDTO>(post);
        await _cacheService.SetAsync(BuildPostCacheKey(post.Id), updatedDto, cancellationToken: cancellationToken);

        return updatedDto;
    }

    private static string BuildPostCacheKey(int postId) => $"{PostCacheKeyPrefix}{postId}";
}
