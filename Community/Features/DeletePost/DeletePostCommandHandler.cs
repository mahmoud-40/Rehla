using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Services.Interface;
using BreastCancer.Context;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace BreastCancer.Community.Features.DeletePost;

public sealed class DeletePostCommandHandler : IRequestHandler<DeletePostCommand, Unit>
{
    private const string PostCacheKeyPrefix = "post:";
    private readonly BreastCancerDB _dbContext;
    private readonly ICacheService _cacheService;

    public DeletePostCommandHandler(BreastCancerDB dbContext, ICacheService cacheService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    public async Task<Unit> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts.FirstOrDefaultAsync(p => p.Id == request.PostId && !p.IsDeleted, cancellationToken);
        if (post is null)
        {
            throw new PostNotFoundException("Post not found.");
        }

        var isModerator = request.Roles.Any(r => string.Equals(r, "MODERATOR", StringComparison.OrdinalIgnoreCase));
        if (!isModerator && !string.Equals(post.AuthorId, request.RequesterId, StringComparison.OrdinalIgnoreCase))
        {
            throw new PostAccessForbiddenException("Only the author or a moderator can delete this post.");
        }

        post.IsDeleted = true;
        post.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cacheService.DeleteAsync(BuildPostCacheKey(post.Id), cancellationToken);

        return Unit.Value;
    }

    private static string BuildPostCacheKey(int postId) => $"{PostCacheKeyPrefix}{postId}";
}
