using AutoMapper;
using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Services.Interface;
using BreastCancer.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Community.Features.GetPost;

public sealed class GetPostQueryHandler : IRequestHandler<GetPostQuery, PostDTO>
{
    private readonly BreastCancerDB _dbContext;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;

    public GetPostQueryHandler(BreastCancerDB dbContext, IMapper mapper, ICacheService cacheService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    public async Task<PostDTO> Handle(GetPostQuery request, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PostId && !p.IsDeleted, cancellationToken);

        if (post is null)
        {
            throw new PostNotFoundException("Post not found.");
        }

        var isAuthor = string.Equals(post.AuthorId, request.RequesterId, StringComparison.OrdinalIgnoreCase);
        if (!isAuthor && !PostVisibilityEvaluator.CanView(post.Visibility, request.Roles))
        {
            throw new PostAccessForbiddenException("You are not allowed to view this post.");
        }

        var dto = _mapper.Map<PostDTO>(post);

        var reactionCounts = await _cacheService.GetHashAllFieldsAsync($"post:{post.Id}:reactions", cancellationToken);
        var commentCounts = await _cacheService.GetHashAllFieldsAsync($"post:{post.Id}:comments", cancellationToken);

        dto.ReactionCounts = reactionCounts;
        dto.CommentCounts = commentCounts;

        return dto;
    }
}
