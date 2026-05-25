using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace BreastCancer.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get paginated notification history for the logged-in user")]
        [SwaggerResponse(StatusCodes.Status200OK, "Returns paginated notifications")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _notificationService.GetUserNotificationsAsync(userId, page, pageSize);
            return Ok(result);
        }

        [HttpPut("{id}/read")]
        [SwaggerOperation(Summary = "Mark a specific notification as read")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Notification marked as read")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Notification not found")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var updated = await _notificationService.MarkAsReadAsync(id, userId);
            if (!updated)
            {
                return NotFound(new { message = "Notification not found." });
            }

            return NoContent();
        }

        [HttpPut("read-all")]
        [SwaggerOperation(Summary = "Mark all notifications as read for the logged-in user")]
        [SwaggerResponse(StatusCodes.Status200OK, "All notifications marked as read")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var count = await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { markedCount = count });
        }
    }
}
