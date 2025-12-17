using BreastCancer.Service.Interface;
using System.Net.Mail;
using System.Net;

namespace BreastCancer.Service.Implementation
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            this._logger = logger;
        }
        public async Task SendEmailAsync(string recipient, string subject, string body)
        {
            try
            {
                IConfigurationSection smtpSettings = _configuration.GetSection("SmtpSettings");

                using MailMessage mailMessage = new()
                {
                    From = new MailAddress(smtpSettings["Username"]!),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(recipient);

                using SmtpClient smtpClient = new(smtpSettings["Server"])
                {
                    Port = int.Parse(smtpSettings["Port"]!),
                    Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
                    EnableSsl = bool.Parse(smtpSettings["EnableSsl"]!)
                };

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Email successfully sent to {Recipient}", recipient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient}", recipient);
                throw; 
            }
        }
    }
}
