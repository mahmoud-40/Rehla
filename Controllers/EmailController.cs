using BreastCancer.DTO.request;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        [HttpPost]
        public async Task SendEmail([FromBody] SendEmailDTO emailDTO) // For testing purposes
        {
            await _emailService.SendEmailAsync(emailDTO.RecipientEmail, emailDTO.Subject, emailDTO.Body);
        }
    }
}
