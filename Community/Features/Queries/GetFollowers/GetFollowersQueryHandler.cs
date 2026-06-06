using System.Globalization;
using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Features.Queries.GetFollowers;
using BreastCancer.Context;
using BreastCancer.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rehla.Community.DTO.response;

namespace Rehla.Community.Features.Queries.GetFollowers
{
    public class GetFollowersQueryHandler : IRequestHandler<GetFollowersQuery, PaginatedFollowerDto>
    {
        private readonly BreastCancerDB _context;
        private readonly UserManager<ApplicationUser> _userManger;

        public GetFollowersQueryHandler(BreastCancerDB Context,UserManager<ApplicationUser> UserManger)
        {
            this._context = Context;
            this._userManger = UserManger;
        }
        public async Task<PaginatedFollowerDto> Handle(GetFollowersQuery request, CancellationToken cancellationToken)
        {
            int limit = Math.Clamp(request.Limit, 1, 100);
            
            int totalFollowersCount = await _context.Follows.AsNoTracking().CountAsync(f => f.FollowingId == request.UserId , cancellationToken);
            
            IQueryable<Follow> followersQuery = _context.Follows.AsNoTracking()
                .Where(f => f.FollowingId == request.UserId)
                .OrderBy(f => f.CreatedAt);

            if (!string.IsNullOrEmpty(request.Cursor) &&
                DateTimeOffset.TryParseExact(request.Cursor, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var cursorOffset))
            {
                followersQuery = followersQuery.Where(follow => follow.CreatedAt > cursorOffset.UtcDateTime);
            }
            
            var followerRecords = await followersQuery.Take(limit + 1)
                .Select(follow => new
                {
                    follow.CreatedAt,
                    UserId         = follow.Follower.Id,
                    DisplayName    = follow.Follower.FullName,
                    AvatarUrl      = follow.Follower.ImageUrl,
                    Role = _context.UserRoles.AsNoTracking()
                        .Where(ur => ur.UserId == follow.FollowerId)
                        .Join(
                            _context.Roles 
                            ,ur => ur.RoleId,
                            r=> r.Id,
                            (_,r)=> r.Name)
                        .FirstOrDefault() ?? "Unknown"
                })
                .ToListAsync(cancellationToken);
            
            // check if there's next page or no
            string? nextCursor = null; 
            bool hasMoreFollowers = followerRecords.Count > limit;
            if (hasMoreFollowers)
            {
                var extraItem = followerRecords.Last();
                nextCursor = extraItem.CreatedAt.ToString("o");
                followerRecords.RemoveAt(followerRecords.Count - 1);
            }

            var followers = followerRecords.Select(date => new FollowerDto
            {
                UserId = date.UserId,
                Name = date.DisplayName,
                AvatarUrl = date.AvatarUrl,
                Role = date.Role
                
            }).ToList();
            
            return new PaginatedFollowerDto
            {
                 Followers = followers,
                 NextCursor = nextCursor,
                 Total = totalFollowersCount
            };
        }
    }
}