using BreastCancer.Community.DTO.request;
using BreastCancer.Community.DTO.response;
using MediatR;

namespace BreastCancer.Community.Features;

public sealed record CreatePostCommand(CreatePostDTO Post, string AuthorId, IReadOnlyCollection<string> Roles) : IRequest<PostDTO>;
