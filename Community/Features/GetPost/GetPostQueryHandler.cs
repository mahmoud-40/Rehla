using AutoMapper;
using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Exceptions;
using BreastCancer.Context;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace BreastCancer.Community.Features.GetPost;

public sealed class GetPostQueryHandler : IRequestHandler<GetPostQuery, PostDTO>
{
    private readonly BreastCancerDB _dbContext;
    private readonly IMapper _mapper;

    public GetPostQueryHandler(BreastCancerDB dbContext, IMapper mapper)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<PostDTO> Handle(GetPostQuery request, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PostId && !p.IsDeleted, cancellationToken);

        if (post is null)
        {
            throw new PostNotFoundException("Post not found.");
        }

        if (!PostVisibilityEvaluator.CanView(post.Visibility, request.Roles))
        {
            throw new PostAccessForbiddenException("You are not allowed to view this post.");
        }

        return _mapper.Map<PostDTO>(post);
    }
}
