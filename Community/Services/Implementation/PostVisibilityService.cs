using BreastCancer.Community.Services.Interface;
using BreastCancer.Context;
using BreastCancer.Models;
using BreastCancer.Enum;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Community.Services.Implementation;

public class PostVisibilityService :IPostVisibilityService
{
    private readonly BreastCancerDB _context;
    public PostVisibilityService(BreastCancerDB context)
    {
        this._context = context;
    }
    
    public async Task<IQueryable<Post>> ApplyVisibilityFilterAsync(
        IQueryable<Post> query,
        string? currentUserId, 
        CancellationToken cancellationToken=default)
    {
        if (string.IsNullOrEmpty(currentUserId))
            return query.Where(post => post.Visibility == PostVisibility.Public);

        var userContext = await GetUserContextAsync(currentUserId, cancellationToken);

        return query.Where(
            post => post.AuthorId == currentUserId ||
            IsVisibleByRole(post.Visibility,userContext.Role) || 
            (post.Visibility == PostVisibility.FollowersOnly && userContext.FollowingIds.Contains(post.AuthorId))
        );

    }

    public async Task<bool> IsPostVisibleAsync(Post post, string? currentUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            return post.Visibility == PostVisibility.Public;
        }

        if (post.AuthorId == currentUserId)
        {
            return true;
        }

        var userContext = await GetUserContextAsync(currentUserId, cancellationToken);

        if (
            IsVisibleByRole(post.Visibility, userContext.Role) ||
            (PostVisibility.FollowersOnly == post.Visibility && userContext.FollowingIds.Contains(post.AuthorId))
        )
        {
            return true;
        }
        
        return false;
    }
    
    
    public async Task<UserContext> GetUserContextAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        var user = await _context.Users
            .AsNoTracking()
            .Select(u => new{
                u.Id,
                IsPatient = u.Patient!=null,
                IsDoctor = u.Doctor != null,
                IsCaregiver = u.Caregiver != null
            }).FirstOrDefaultAsync(u => u.Id == userId,cancellationToken);

        var role = user?.IsPatient == true ?
            "Patient" : user?.IsDoctor == true ?
            "Doctor" : user?.IsCaregiver == true ?
            "Caregiver" : "Unknown" ;

        var followingIds = await _context.Follows.AsNoTracking()
            .Where(u => u.FollowerId == userId)
            .Select(f => f.FollowingId)
            .AsAsyncEnumerable()
            .ToHashSetAsync();

        return new UserContext(role,followingIds);
    }

    private static bool IsVisibleByRole(PostVisibility visibility, string role)
    {
        return visibility switch
        {
            PostVisibility.Public => true,
            PostVisibility.DoctorOnly => role == "Doctor",
            PostVisibility.PatientsOnly => role == "Patient",
            PostVisibility.CaregiverOnly => role == "Caregiver",
            _ => false
        };
    }
    public record UserContext(string Role, HashSet<string> FollowingIds);

}