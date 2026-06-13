using System.Security.Claims;
using BreastCancer.Community.DTO.request;
using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Features.Commands.AddReaction;
using BreastCancer.Community.Features.Commands.RemoveReaction;
using BreastCancer.Community.Features.CreatePost;
using BreastCancer.Community.Features.DeletePost;
using BreastCancer.Community.Features.Feed;
using BreastCancer.Community.Features.FollowUser;
using BreastCancer.Community.Features.GetPost;
using BreastCancer.Community.Features.Queries.GetFollowers;
using BreastCancer.Community.Features.Queries.GetFollowing;
using BreastCancer.Community.Features.UnfollowUser;
using BreastCancer.Community.Features.UpdatePost;
using BreastCancer.Community.Security;
using BreastCancer.Enum;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rehla.Community.DTO.response;
using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Features.Queries.GetUserPosts;
using Swashbuckle.AspNetCore.Annotations;

namespace BreastCancer.Community.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommunityController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CommunityController(IMediator mediator)
        {
            this._mediator = mediator;
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
                var post = await _mediator.Send(new CreatePostCommand(createPostDTO, userId, roles));
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
                await _mediator.Send(new FollowUserCommand(followerId, userId));
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
                await _mediator.Send(new UnfollowUserCommand(followerId, userId));
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

            var feed = await _mediator.Send(new GetFeedQuery(userId, cursor, effectiveLimit, roles), cancellationToken);
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
                var post = await _mediator.Send(new GetPostQuery(postId, userId, roles), cancellationToken);
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
                var post = await _mediator.Send(new UpdatePostCommand(postId, updatePostDto, userId), cancellationToken);
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
                await _mediator.Send(new DeletePostCommand(postId, userId, roles), cancellationToken);
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

        [HttpGet("{userId}/followers")]
        [ProducesResponseType(typeof(PaginatedFollowerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PaginatedFollowerDto>> GetFollowers(
            [FromRoute] string userId,
            [FromQuery] string? cursor = null,
            [FromQuery] int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("User ID cannot be empty");

            var query = new GetFollowersQuery(userId, cursor, limit);
            var result = await _mediator.Send(query);
            return Ok(result);
        }


        [HttpGet("{userId}/followings")]
        [ProducesResponseType(typeof(PaginatedFollowerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PaginatedFollowerDto>> GetFollowing([FromRoute] string userId,
            [FromQuery] string? cursor = null,
            [FromQuery] int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("User ID cannot be empty");
            var query = new GetFollowingQuery(userId, cursor, limit);
            var result = await _mediator.Send(query);
            
            return Ok(result);
        }

        [HttpGet("{userId}/posts")]
        [Authorize]
        [ProducesResponseType(typeof(UserPostsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserPostsResponseDto>> GetUserPosts(
            [FromRoute] string userId,
            [FromQuery] string? cursor = null,
            [FromQuery] int limit =10
        )
        {
             if (string.IsNullOrEmpty(userId))
                return BadRequest(new { error = "User Id cannot be empty" });

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var query = new GetUserPostsQuery(userId, cursor, limit, currentUserId);

            try
            {
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message,
                    trace = ex.StackTrace
                });
            }
        }
        [HttpPost("posts/{postId:int}/reactions")]
        [Authorize]
        [SwaggerOperation(Summary = "Add a reaction to a post")]
        [SwaggerResponse(StatusCodes.Status200OK, "Reaction added successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation error")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Forbidden")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Post not found")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Conflict - User has already reacted to this post")] 
        public async Task<IActionResult> AddReaction([FromRoute] int postId, [FromQuery] ReactionType type, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Could not identify user" });
            }

            var roles = User.GetRoles();

            try
            {
                await _mediator.Send(new AddReactionCommand(postId, type, userId, roles), cancellationToken);
                return Ok(new { message = "Reaction added successfully" });
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
            catch (DuplicateReactionException ex)
            {
                return Conflict(new { message = ex.Message });
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

        [HttpDelete("posts/{postId:int}/reactions")]
        [Authorize]
        [SwaggerOperation(Summary = "Remove a reaction from a post")]
        [SwaggerResponse(StatusCodes.Status200OK, "Reaction removed successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation error")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Forbidden")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "User has not reacted to this post")]
        public async Task<IActionResult> RemoveReaction([FromRoute] int postId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Could not identify user" });
            }
            try
            {
                await _mediator.Send(new RemoveReactionCommand(postId, userId), cancellationToken);
                return Ok(new { message = "Reaction removed successfully" });
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
            catch (ReactionNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (PostAccessForbiddenException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }
        [HttpGet("debug-claims")]
        [Authorize]
        public IActionResult DebugClaims()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value });
            return Ok(claims);
        }
    }
}
