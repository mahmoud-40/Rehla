using BreastCancer.Community.DTO.request;
using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Features;
using BreastCancer.Community.Security;
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
            catch (PostAccessForbiddenException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }

    }
}
