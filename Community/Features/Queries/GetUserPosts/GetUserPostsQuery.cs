using BreastCancer.Community.DTO.response;
using MediatR;

namespace BreastCancer.Community.Features.Queries.GetUserPosts;

public record GetUserPostsQuery(
        string UserId,
        string? Cursor,
        int Limit =10,
        string? CurrentUserId = null
    ) : IRequest<UserPostsResponseDto>;