using BreastCancer.Models;

namespace BreastCancer.Community.Services.Interface;

public interface IPostVisibilityService
{
    Task<IQueryable<Post>> ApplyVisibilityFilterAsync(
        IQueryable<Post> query, 
        string? currentUserId,
        CancellationToken cancellationToken = default
    );

    Task<bool> IsPostVisibleAsync(
        Post post,
        string? currentUserId,
        CancellationToken cancellationToken = default
    );
}