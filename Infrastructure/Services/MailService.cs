using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using Trailblazers.Backend.Core.Application.Interfaces;

namespace Trailblazers.Backend.Infrastructure.Services
{
    public class MailService(ILogger<MailService> logger) : IMailService
    {
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "localhost";
            var portStr = Environment.GetEnvironmentVariable("SMTP_PORT");
            var username = Environment.GetEnvironmentVariable("SMTP_USERNAME");
            var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
            var from = Environment.GetEnvironmentVariable("SMTP_FROM") ?? "noreply@trailblazers.com";

            int port = 587; // default port
            if (!string.IsNullOrWhiteSpace(portStr) && int.TryParse(portStr, out var parsedPort))
            {
                port = parsedPort;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Trailblazers", from));
            message.To.Add(new MailboxAddress("Recipient", to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { TextBody = body };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                // Strictly enforce TLS certificate validation in production.
                // Only bypass if explicitly requested for local dev test servers (e.g. Mailpit/Maildev)
                var allowInvalidCert = string.Equals(
                    Environment.GetEnvironmentVariable("SMTP_ALLOW_INVALID_CERT"),
                    "true",
                    StringComparison.OrdinalIgnoreCase);

                if (allowInvalidCert)
                {
                    logger.LogWarning("SECURITY WARNING: SMTP TLS certificate validation is disabled via SMTP_ALLOW_INVALID_CERT=true.");
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                }

                // Determine secure socket options
                var secureOptions = SecureSocketOptions.Auto;
                if (port == 465)
                {
                    secureOptions = SecureSocketOptions.SslOnConnect;
                }
                else if (port == 587)
                {
                    secureOptions = SecureSocketOptions.StartTls;
                }

                await client.ConnectAsync(host, port, secureOptions);

                if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                {
                    await client.AuthenticateAsync(username, password);
                }

                await client.SendAsync(message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SMTP Exception] Failed to send email to {RecipientEmail}", to);
                throw;
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}
