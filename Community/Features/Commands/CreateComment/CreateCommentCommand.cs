using BreastCancer.Community.DTO.request;
using BreastCancer.Community.DTO.response;
using MediatR;

namespace Rehla.Community.Features.Commands.CreateComment;

public record CreateCommentCommand(CreateCommentDTO comment , string authorId,int postId , CancellationToken cancellationToken) : IRequest<CommentResponseDTO>;