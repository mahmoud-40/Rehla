using BreastCancer.Community.DTO.response;
using MediatR;

namespace BreastCancer.Community.Features.Posts;

public sealed record GetPostQuery(int PostId, string RequesterId, IReadOnlyCollection<string> Roles) : IRequest<PostDTO>;
