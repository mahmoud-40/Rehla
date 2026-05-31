using BreastCancer.Community.DTO.request;
using BreastCancer.Community.DTO.response;
using MediatR;

namespace BreastCancer.Community.Features.Posts;

public sealed record UpdatePostCommand(int PostId, UpdatePostDTO Post, string RequesterId) : IRequest<PostDTO>;
