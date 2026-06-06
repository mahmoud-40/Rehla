using MediatR;
using Rehla.Community.DTO.response;

namespace BreastCancer.Community.Features.Queries.GetFollowing
{
    public record GetFollowingQuery(
        string UserId,
        string? Cursor,
        int Limit = 20
    ) : IRequest<PaginatedFollowerDto> ;
    
}
