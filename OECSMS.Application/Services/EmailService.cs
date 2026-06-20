using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OECSMS.Contracts;

namespace OECSMS.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // For development, we log the email instead of actually sending it via SMTP.
            _logger.LogInformation("========== MOCK EMAIL SENT ==========");
            _logger.LogInformation($"To: {toEmail}");
            _logger.LogInformation($"Subject: {subject}");
            _logger.LogInformation($"Body:\n{body}");
            _logger.LogInformation("=====================================");

            return Task.CompletedTask;
        }
    }
}
