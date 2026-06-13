using System.Globalization;
using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Services.Interface;
using BreastCancer.Context;
using BreastCancer.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Community.Features.Queries.GetUserPosts;

public class GetUserPostsQueryHandler : IRequestHandler<GetUserPostsQuery,UserPostsResponseDto>
{
    private readonly BreastCancerDB _context;
    private readonly IPostVisibilityService _postVisibilityService;
    public GetUserPostsQueryHandler(BreastCancerDB context,IPostVisibilityService postVisibilityService)
    {
        this._context = context;
        this._postVisibilityService = postVisibilityService;
    }
    public async Task<UserPostsResponseDto> Handle(GetUserPostsQuery request, CancellationToken cancellationToken)
    {
        var userExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            throw new NotFoundException($"User with ID '{request.UserId}' was not found");
        }
        
        int limit = Math.Clamp(request.Limit, 1, 50);
        IQueryable<Post> postsQuery = _context.Posts
            .AsNoTracking()
            .Where(p => p.AuthorId == request.UserId)
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt);

        bool isViewingOwnPosts = request.CurrentUserId == request.UserId;

        if (!isViewingOwnPosts)
        {
            postsQuery = await _postVisibilityService
                .ApplyVisibilityFilterAsync(postsQuery, request.CurrentUserId, cancellationToken);
        }

        if (!string.IsNullOrEmpty(request.Cursor))
        {
            if (DateTimeOffset
                .TryParseExact(request.Cursor,"o" ,CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                    out var cursorDate)
               )
            {
                postsQuery = postsQuery.Where(p => p.CreatedAt < cursorDate.UtcDateTime);
            }
        }

        var postsTask =  postsQuery.Take(limit + 1)
            .Select(p => new
            {
                p.Id,
                p.Content,
                p.Type,
                p.MediaUrls,
                p.CreatedAt,
                p.UpdatedAt,
                p.AuthorId,
                p.Visibility,
                AuthorName = p.Author.FullName,
                AuthorAvatarUrl = p.Author.ImageUrl,
                AuthorRole = p.Author.Patient != null ? "Patient" : p.Author.Doctor != null ? "Doctor" : p.Author.Caregiver != null ? "Caregiver" : "Unknown",
                ReactionsCount = p.Reactions.Count,
                CommentsCount = p.Comments.Count(c=> !c.IsDeleted),
                IsLikedByCurrentUser = !string.IsNullOrEmpty(request.CurrentUserId) && p.Reactions.Any(r => r.UserId == request.CurrentUserId),
                IsEdited = p.UpdatedAt != null && p.UpdatedAt > p.CreatedAt
            }).ToListAsync(cancellationToken);
        var totalTask =  postsQuery.CountAsync(cancellationToken);
        await Task.WhenAll(postsTask,totalTask);
        
        var postsData = postsTask.Result;
        var totalCount = totalTask.Result;
        
        string? nextCursor = null;
        bool hasMorePosts = postsData.Count > limit;

        if (hasMorePosts)
        {
            nextCursor = postsData[limit].CreatedAt.ToString("o");
            postsData.RemoveAt(limit);
        }

        var posts = postsData.Select(p => new PostDTO
        {
            Id = p.Id,
            Content = p.Content,
            PostType = p.Type,
            ImageUrl = p.MediaUrls,
            AuthorId = p.AuthorId,
            AuthorName = p.AuthorName,
            AuthorAvatarUrl = p.AuthorAvatarUrl,
            AuthorRole = p.AuthorRole,
            PostVisibility = p.Visibility,
            ReactionsCount = p.ReactionsCount,
            CommentsCount = p.CommentsCount,
            IsLikedByCurrentUser = p.IsLikedByCurrentUser,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            IsEdited = p.IsEdited
        }).ToList();

        return new UserPostsResponseDto
        {
            Posts = posts,
            NextCursor = nextCursor,
            Total = totalCount
        };
    }
}