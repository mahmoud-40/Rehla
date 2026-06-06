using MediatR;
using Rehla.Community.DTO.response;

namespace BreastCancer.Community.Features.Queries.GetFollowers
{
    public record GetFollowersQuery(
        string UserId,
        string? Cursor,
        int Limit = 20
    ) : IRequest<PaginatedFollowerDto> ;
}

