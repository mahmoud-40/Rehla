using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Services.Interface;
using BreastCancer.Context;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace BreastCancer.Community.Features.DeletePost;

public sealed class DeletePostCommandHandler : IRequestHandler<DeletePostCommand, Unit>
{
    private const string PostCacheKeyPrefix = "post:";
    private const string FeedKeyPrefix = "feed:";

    private readonly BreastCancerDB _dbContext;
    private readonly ICacheService _cacheService;

    public DeletePostCommandHandler(BreastCancerDB dbContext, ICacheService cacheService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    public async Task<Unit> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts
            .FirstOrDefaultAsync(p => p.Id == request.PostId && !p.IsDeleted, cancellationToken);

        if (post is null)
            throw new PostNotFoundException("Post not found.");

        var isModerator = request.Roles.Any(r => string.Equals(r, "MODERATOR", StringComparison.OrdinalIgnoreCase));
        if (!isModerator && !string.Equals(post.AuthorId, request.RequesterId, StringComparison.OrdinalIgnoreCase))
            throw new PostAccessForbiddenException("Only the author or a moderator can delete this post.");

        post.IsDeleted = true;
        post.UpdatedAt = DateTime.UtcNow;

        var followerIds = await _dbContext.Follows
            .AsNoTracking()
            .Where(f => f.FollowingId == post.AuthorId)
            .Select(f => f.FollowerId)
            .ToListAsync(cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cacheService.DeleteAsync(BuildPostCacheKey(post.Id), cancellationToken);

        _ = Task.Run(async () =>
        {
            var postIdMember = new[] { post.Id.ToString() };
            var tasks = followerIds.Select(followerId =>
                _cacheService.RemoveFromSortedSetAsync(
                    BuildFeedKey(followerId),
                    postIdMember,
                    CancellationToken.None)
            );
            await Task.WhenAll(tasks);
        }, CancellationToken.None);

        return Unit.Value;
    }

    private static string BuildPostCacheKey(int postId) => $"{PostCacheKeyPrefix}{postId}";
    private static string BuildFeedKey(string userId) => $"{FeedKeyPrefix}{userId}";
}