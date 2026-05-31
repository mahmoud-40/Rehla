using BreastCancer.Community.DTO.request;
using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Features;
using BreastCancer.Community.Features.Feed;
using BreastCancer.Community.Features.Posts;
using BreastCancer.Community.DTO.request;
using BreastCancer.Community.Features.CreatePost;
using BreastCancer.Community.Features.FollowUser;
using BreastCancer.Community.Features.UnfollowUser;
using BreastCancer.Community.Security;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BreastCancer.Community.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommunityController : ControllerBase
    {
        private readonly IMediator mediator;

        public CommunityController(IMediator mediator)
        {
            this.mediator = mediator;
        }

        [HttpPost("posts")]
        [Authorize]
        [SwaggerOperation(Summary = "Create a community post")]
        [SwaggerResponse(StatusCodes.Status201Created, "Post created successfully")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Forbidden - user role not allowed")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation error")]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostDTO createPostDTO)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Could not identify user" });
            }

            var roles = User.GetRoles();
            try
            {
                var post = await mediator.Send(new CreatePostCommand(createPostDTO, userId, roles));
                return StatusCode(StatusCodes.Status201Created, post);
            }
            catch (ValidationException ex)
            {
                var errors = ex.Errors.Select(error => new
                {
                    field = error.PropertyName,
                    message = error.ErrorMessage
                });
                return BadRequest(new { errors });
            }
            catch (PostAccessForbiddenException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }

        [HttpPost("users/{userId}/follow")]
        [Authorize]
        [SwaggerOperation(Summary = "Follow a user")]
        [SwaggerResponse(StatusCodes.Status200OK, "User followed successfully")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "User not found")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Already following the user")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation error")]
        public async Task<IActionResult> FollowUser([FromRoute] string userId)
        {
            var followerId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(followerId))
            {
                return Unauthorized(new { message = "Could not identify user" });
            }
            try
            {
                await mediator.Send(new FollowUserCommand(followerId, userId));
                return Ok(new { message = "User followed successfully" });
            }
            catch (ValidationException ex)
            {
                var errors = ex.Errors.Select(error => new
                {
                    field = error.PropertyName,
                    message = error.ErrorMessage
                });
                return BadRequest(new { errors });
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (AlreadyFollowingException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpDelete("users/{userId}/follow")]
        [Authorize]
        [SwaggerOperation(Summary = "Unfollow a user")]
        [SwaggerResponse(StatusCodes.Status200OK, "User unfollowed successfully")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Follow relationship not found")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation error")]
        public async Task<IActionResult> UnfollowUser([FromRoute] string userId)
        {
            var followerId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(followerId))
            {
                return Unauthorized(new { message = "Could not identify user" });
            }
            try
            {
                await mediator.Send(new UnfollowUserCommand(followerId, userId));
                return Ok(new { message = "User unfollowed successfully" });
            }
            catch (ValidationException ex)
            {
                var errors = ex.Errors.Select(error => new
                {
                    field = error.PropertyName,
                    message = error.ErrorMessage
                });
                return BadRequest(new { errors });
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("feed")]
        [Authorize]
        [SwaggerOperation(Summary = "Get community feed")]
        [SwaggerResponse(StatusCodes.Status200OK, "Feed retrieved successfully")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        public async Task<IActionResult> GetFeed([FromQuery] int? cursor, [FromQuery] int? limit, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Could not identify user" });
            }

            var roles = User.GetRoles();

            var effectiveLimit = limit ?? 20;
            if (effectiveLimit > 50)
            {
                effectiveLimit = 50;
            }
            if (effectiveLimit < 1)
            {
                effectiveLimit = 1;
            }

            var feed = await mediator.Send(new GetFeedQuery(userId, cursor, effectiveLimit, roles), cancellationToken);
            return Ok(feed);
        }

        [HttpGet("posts/{postId:int}")]
        [Authorize]
        [SwaggerOperation(Summary = "Get a community post")]
        [SwaggerResponse(StatusCodes.Status200OK, "Post retrieved successfully")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Forbidden")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Post not found")]
        public async Task<IActionResult> GetPost([FromRoute] int postId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Could not identify user" });
            }

            var roles = User.GetRoles();
            try
            {
                var post = await mediator.Send(new GetPostQuery(postId, userId, roles), cancellationToken);
                return Ok(post);
            }
            catch (PostNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (PostAccessForbiddenException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }

        [HttpPut("posts/{postId:int}")]
        [Authorize]
        [SwaggerOperation(Summary = "Update a community post")]
        [SwaggerResponse(StatusCodes.Status200OK, "Post updated successfully")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Forbidden")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Post not found")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation error")]
        public async Task<IActionResult> UpdatePost([FromRoute] int postId, [FromBody] UpdatePostDTO updatePostDto, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Could not identify user" });
            }

            try
            {
                var post = await mediator.Send(new UpdatePostCommand(postId, updatePostDto, userId), cancellationToken);
                return Ok(post);
            }
            catch (ValidationException ex)
            {
                var errors = ex.Errors.Select(error => new
                {
                    field = error.PropertyName,
                    message = error.ErrorMessage
                });
                return BadRequest(new { errors });
            }
            catch (PostNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (PostAccessForbiddenException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }

        [HttpDelete("posts/{postId:int}")]
        [Authorize]
        [SwaggerOperation(Summary = "Delete a community post")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Post deleted successfully")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Forbidden")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Post not found")]
        public async Task<IActionResult> DeletePost([FromRoute] int postId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Could not identify user" });
            }

            var roles = User.GetRoles();
            try
            {
                await mediator.Send(new DeletePostCommand(postId, userId, roles), cancellationToken);
                return NoContent();
            }
            catch (PostNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (PostAccessForbiddenException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }
    }
}
