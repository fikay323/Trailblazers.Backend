using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using Trailblazers.Backend.Core.Application.Interfaces;

namespace Trailblazers.Backend.Infrastructure.Services
{
    public class MailService(ILogger<MailService> logger) : IMailService
    {
        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            var host = Environment.GetEnvironmentVariable("SMTP_HOST");
            var portStr = Environment.GetEnvironmentVariable("SMTP_PORT");
            var username = Environment.GetEnvironmentVariable("SMTP_USERNAME");
            var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
            var from = Environment.GetEnvironmentVariable("SMTP_FROM") ?? "noreply@trailblazers.com";
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            bool isDev = string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(host) || (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) && !isDev))
            {
                logger.LogWarning("SMTP is not configured in environment (SMTP_HOST is empty or localhost). Skipped sending email to {RecipientEmail}. If deploying on Render, set SMTP_HOST, SMTP_PORT, SMTP_USERNAME, and SMTP_PASSWORD in Render environment variables.", to);
                throw new InvalidOperationException("SMTP host is not configured in production environment variables.");
            }

            int port = 587; // default port
            if (!string.IsNullOrWhiteSpace(portStr) && int.TryParse(portStr, out var parsedPort))
            {
                port = parsedPort;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Trailblazers", from));
            message.To.Add(new MailboxAddress("Recipient", to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isHtml)
            {
                bodyBuilder.HtmlBody = body;
            }
            else
            {
                bodyBuilder.TextBody = body;
            }
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            // Cap socket connection/read/write timeout to 6 seconds so requests never hang for 2 minutes
            client.Timeout = 6000;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));

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

                await client.ConnectAsync(host, port, secureOptions, cts.Token);

                if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                {
                    await client.AuthenticateAsync(username, password, cts.Token);
                }

                await client.SendAsync(message, cts.Token);
            }
            catch (Exception ex) when (ex is TimeoutException or OperationCanceledException)
            {
                logger.LogError("[SMTP Timeout] Timed out connecting to '{Host}:{Port}'. NOTE: Cloud providers like Render block outbound SMTP ports (25, 465, 587) on free plans.", host, port);
                throw new TimeoutException($"SMTP connection to {host}:{port} timed out after 6 seconds. Render or hosting provider may be blocking port {port}.", ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SMTP Exception] Failed to send email to {RecipientEmail}", to);
                throw;
            }
            finally
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true);
                }
            }
        }
    }
}
