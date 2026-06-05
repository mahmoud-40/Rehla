using System.Globalization;
using BreastCancer.Community.DTO.response;
using BreastCancer.Context;
using BreastCancer.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Rehla.Community.DTO.response;

namespace BreastCancer.Community.Features.Queries.GetFollowing;

public class GetFollowerQueryHandler : IRequestHandler<GetFollowingQuery,PaginatedFollowerDto>
{
    private readonly BreastCancerDB _context;

    public GetFollowerQueryHandler(BreastCancerDB Context)
    {
        this._context = Context;
    }
    
    public async Task<PaginatedFollowerDto> Handle(GetFollowingQuery request, CancellationToken cancellationToken)
    {
        int limit = Math.Clamp(request.Limit, 1, 100);
        int TotalFollowingCount = await _context.Follows.AsNoTracking()
            .CountAsync(f => f.FollowerId == request.UserId , cancellationToken);

        IQueryable<Follow> followingQuery = _context.Follows.AsNoTracking()
            .Where(f => f.FollowerId == request.UserId)
            .OrderBy(f => f.CreatedAt);

        if (!string.IsNullOrEmpty(request.Cursor)
            && DateTimeOffset.TryParseExact(request.Cursor, "o", CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var cursorOffset))
        {
            followingQuery = followingQuery.Where(follow => follow.CreatedAt > cursorOffset);
        }

        var followingRecords = await followingQuery
            .Take(limit + 1)
            .Select(follow => new
            {
                follow.CreatedAt,
                UserId = follow.Following.Id,
                DisplayName = follow.Following.FullName,
                AvatarUrl = follow.Following.ImageUrl,
                Role = _context.UserRoles
                    .Where(ur => ur.UserId == follow.FollowingId)
                    .Join(_context.Roles,
                        ur => ur.RoleId,
                        r => r.Id,
                        (_,r)=> r.Name)
                    .FirstOrDefault() ?? "Unknown"
            }).ToListAsync(cancellationToken);

        string? nextCursor = null;
        bool hasMoreFollowing = followingRecords.Count > limit;
        if (hasMoreFollowing)
        {
            nextCursor = followingRecords.Last().CreatedAt.ToString("o");
            followingRecords.RemoveAt(followingRecords.Count -1);
        }

        var followings = followingRecords.Select(data => new FollowerDto
        {
            UserId = data.UserId,
            Name = data.DisplayName,
            AvatarUrl = data.AvatarUrl,
            Role = data.Role
        }).ToList();
        return new PaginatedFollowerDto
        {
            Followers = followings,
            NextCursor = nextCursor,
            Total = TotalFollowingCount
        };
    }
}