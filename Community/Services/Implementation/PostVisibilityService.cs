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
        var followingIds = userContext.FollowingIds;

        return query.Where(
            post => post.AuthorId == currentUserId ||
            IsVisibleByRole(post.Visibility,userContext.Role) || 
            (post.Visibility == PostVisibility.FollowersOnly && followingIds.Contains(post.AuthorId))
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

        return IsVisibleByUserContext(post.Visibility, post.AuthorId , userContext);

    }
    
    
    private async Task<UserContext> GetUserContextAsync(
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

        
        var followingIds =  await _context.Follows.AsNoTracking()
            .Where(u => u.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToListAsync(cancellationToken);

        
        var role = user?.IsPatient == true ?
            "Patient" : user?.IsDoctor == true ?
            "Doctor" : user?.IsCaregiver == true ?
            "Caregiver" : "Unknown" ;
        return new UserContext(role,followingIds);
    }

    private static bool IsVisibleByUserContext(PostVisibility visibility, string authorId , UserContext userContext)
    {
        return IsVisibleByRole(visibility, userContext.Role) || (visibility == PostVisibility.FollowersOnly && userContext.FollowingIds.Contains(authorId));
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
    private record UserContext(string Role, List<string> FollowingIds);

}