using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using NexusAstralis.Interface;
using NexusAstralis.Models.Email;

namespace NexusAstralis.Email
{
    public class EmailSender(IOptions<SmtpSettings> smtpSettings) : IEmailSender
    {
        private readonly SmtpSettings _smtpSettings = smtpSettings.Value;

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
            {
                Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpSettings.Username!),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage);
        }
    }
}