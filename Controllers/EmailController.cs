using BreastCancer.DTO.request;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BreastCancer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        /// <summary>
        /// Send an email (for testing purposes)
        /// </summary>
        /// <param name="emailDTO">Email data including recipient, subject, and body</param>
        /// <returns>Success status</returns>
        /// <remarks>
        /// Sends an email using the configured SMTP settings. This endpoint is for testing purposes only.
        /// </remarks>
        [HttpPost]
        [SwaggerOperation(Summary = "Send an email (for testing purposes)")]
        [SwaggerResponse(StatusCodes.Status200OK, "Email sent successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid email data or validation errors")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal server error while sending email")]
        public async Task SendEmail([FromBody] SendEmailDTO emailDTO) // For testing purposes
        {
            await _emailService.SendEmailAsync(emailDTO.RecipientEmail, emailDTO.Subject, emailDTO.Body);
        }
    }
}
