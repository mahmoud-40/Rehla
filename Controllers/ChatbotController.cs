using BreastCancer.DTO.request;
using BreastCancer.Service.Exceptions;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace BreastCancer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(IChatbotService chatbotService, ILogger<ChatbotController> logger)
        {
            _chatbotService = chatbotService;
            _logger = logger;
        }

        [HttpPost("ask")]
        [SwaggerOperation(Summary = "Ask the chatbot a question")]
        [SwaggerResponse(StatusCodes.Status200OK, "Returns the chatbot's response")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Patient diagnosis not found")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "User not authenticated")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "User cannot access this patient's data")]
        [SwaggerResponse(StatusCodes.Status502BadGateway, "Upstream chatbot error")]
        [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Chatbot service unavailable")]
        [SwaggerResponse(StatusCodes.Status504GatewayTimeout, "Chatbot service timeout")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Server error")]
        public async Task<IActionResult> AskChatbot([FromBody] ChatbotAskDTO request)
        {
            try
            {
                var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Could not extract UserId from claims");
                    return Unauthorized(new { message = "Could not identify user" });
                }

                if (userId != request.PatientId)
                {
                    _logger.LogWarning("User {UserId} attempted to access patient data for {PatientId}", userId, request.PatientId);
                    return Forbid();
                }

                var response = await _chatbotService.AskQuestion(request);
                return Ok(response);
            }
            catch (ChatbotDiagnosisNotFoundException ex)
            {
                _logger.LogWarning(ex, "Patient diagnosis not found in chatbot request");
                return NotFound(new { message = ex.Message });
            }
            catch (ChatbotExternalServiceTimeoutException ex)
            {
                _logger.LogWarning(ex, "Chatbot service timeout");
                return StatusCode(StatusCodes.Status504GatewayTimeout, new { message = ex.Message });
            }
            catch (ChatbotExternalServiceException ex)
            {
                _logger.LogWarning(ex, "Chatbot service failure");
                if (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
                }

                return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in chatbot endpoint");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An unexpected error occurred while processing your request" });
            }
        }
    }
}
