using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Events.Models;
using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Services.Interface;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using BreastCancer.Repository.Repositories;
using BreastCancer.Service.Interface;
using MediatR;

namespace Rehla.Community.Features.Commands.CreateComment
{
    public class CreateCommentCommandHandler(
        IUnitOfWork _unitOfWork,
        IPublisher _publisher, 
        IPostVisibilityService _postVisibilityService,
        ICacheService _cacheService) : IRequestHandler<CreateCommentCommand,CommentResponseDTO>
    {
        public async Task<CommentResponseDTO> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
        {
            Post post = await _unitOfWork.PostRepository.GetByIdAsync(request.postId);
            if (post == null || post.IsDeleted)
            {
                throw new PostNotFoundException($"Post with ID {request.postId} does not exist or has been deleted.");
            }

            if (!await _postVisibilityService.IsPostVisibleAsync(post, request.authorId, cancellationToken))
            {
                throw new CommentAccessForbiddenException("You do not have permission to comment on this post.");
            }   
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            var committed = false;
            Comment comment = null;
            try
            {
                
                comment = new Comment
                {
                    AuthorId = request.authorId,
                    PostId = request.postId,
                    Content = request.comment.Content,
                    ImageUrl = request.comment.ImageUrl
                };
                await _unitOfWork.CommentRepository.AddCommentAsync(comment);
                await _unitOfWork.SaveAsync();
                
                await transaction.CommitAsync(cancellationToken);
                committed = true;
            }
            catch
            {
                if (!committed)
                {
                    await transaction.RollbackAsync();
                }
                throw;
            }
            var savedComment = await _unitOfWork.CommentRepository.GetByIdWithIncludesAsync(comment.Id);
            var commentDTO = new CommentResponseDTO
            {
                AuthorId = savedComment.AuthorId,
                Content = savedComment.Content,
                PostId = savedComment.PostId,
                ImageUrl = savedComment.ImageUrl,
                CommentId = savedComment.Id,
                AuthorName =  savedComment.Author.FullName,
                AuthorAvaterUrl = savedComment.Author.ImageUrl,
                CreatedAt = savedComment.CreatedAt
            };
            await _cacheService.SetAsync(BuildCommentCacheKey(comment.PostId,comment.Id), commentDTO, ttl:TimeSpan.FromHours(1), cancellationToken);
            await _cacheService.IncrementHashFieldAsync(
                key: $"post:{comment.PostId}:comments",       
                field: "comment-count",              
                incrementBy: 1,
                cancellationToken: cancellationToken);

              
            await _publisher.Publish(
                new CommentCreatedEvent(comment.Id, comment.PostId, comment.AuthorId,post.AuthorId ,comment.Content,
                    comment.CreatedAt), cancellationToken);
            return commentDTO;
        }
        private static string BuildCommentCacheKey(int postId, int commentId)
            => $"post:{postId}:comment:{commentId}";
        
    }
}