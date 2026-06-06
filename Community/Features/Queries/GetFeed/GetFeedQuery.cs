using BreastCancer.Community.DTO.response;
using MediatR;

namespace BreastCancer.Community.Features.Feed;

public sealed record GetFeedQuery(string UserId, int? Cursor, int Limit, IReadOnlyCollection<string>? Roles = null) : IRequest<FeedResponseDto>;
