using BreastCancer.Community.DTO.response;
using MediatR;

namespace BreastCancer.Community.Features.Queries.SearchUsers
{
    public record SearchUsersQuery(string SearchTerm, int Limit = 20) : IRequest<List<UserSearchDto>>;
}
