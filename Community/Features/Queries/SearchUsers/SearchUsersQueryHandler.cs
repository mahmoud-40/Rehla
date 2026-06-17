using BreastCancer.Community.DTO.response;
using BreastCancer.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Community.Features.Queries.SearchUsers
{
    public class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, List<UserSearchDto>>
    {
        private readonly BreastCancerDB _context;

        public SearchUsersQueryHandler(BreastCancerDB context)
        {
            _context = context;
        }

        public async Task<List<UserSearchDto>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();

                query = query.Where(u =>
                    u.Id == term || 
                    (u.FirstName + " " + u.LastName).Contains(term) ||
                    u.Email.Contains(term));
            }

            var users = await query
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Take(request.Limit)
                .Select(u => new UserSearchDto(
                    u.Id,
                    u.FirstName + " " + u.LastName,
                    u.Email,
                    u.ImageUrl))
                .ToListAsync(cancellationToken);

            return users;
        }
    }
}