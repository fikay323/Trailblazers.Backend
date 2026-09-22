using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using Trailblazers.Backend.Core.Application.Interfaces;

namespace Trailblazers.Backend.Infrastructure.Services
{
    public class MailService(
        IHttpClientFactory httpClientFactory,
        ILogger<MailService> logger) : IMailService
    {
        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            // 1. Priority 1: Resend HTTP REST API (Port 443 - Recommended for Render)
            var resendApiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY");
            if (!string.IsNullOrWhiteSpace(resendApiKey))
            {
                await SendViaResendAsync(resendApiKey.Trim(), to, subject, body, isHtml);
                return;
            }

            // 2. Priority 2: Brevo (Sendinblue) HTTP REST API (Port 443)
            var brevoApiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY");
            if (!string.IsNullOrWhiteSpace(brevoApiKey))
            {
                await SendViaBrevoAsync(brevoApiKey.Trim(), to, subject, body, isHtml);
                return;
            }

            // 3. Priority 3: Fallback to standard SMTP (Port 587/465)
            await SendViaSmtpAsync(to, subject, body, isHtml);
        }

        private async Task SendViaResendAsync(string apiKey, string to, string subject, string body, bool isHtml)
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var from = Environment.GetEnvironmentVariable("EMAIL_FROM")
                    ?? Environment.GetEnvironmentVariable("RESEND_FROM")
                    ?? Environment.GetEnvironmentVariable("SMTP_FROM")
                    ?? "Trailblazers Academy <onboarding@resend.dev>";

            var payload = new Dictionary<string, object>
            {
                ["from"] = from,
                ["to"] = new[] { to },
                ["subject"] = subject
            };

            if (isHtml)
            {
                payload["html"] = body;
            }
            else
            {
                payload["text"] = body;
            }

            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.resend.com/emails", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("[Resend API Error] Status: {StatusCode}, Details: {Details}", response.StatusCode, errorBody);
                throw new HttpRequestException($"Resend email delivery failed (HTTP {(int)response.StatusCode}): {errorBody}");
            }

            logger.LogInformation("Email successfully sent to {Recipient} via Resend HTTP API (Port 443)", to);
        }

        private async Task SendViaBrevoAsync(string apiKey, string to, string subject, string body, bool isHtml)
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Add("api-key", apiKey);

            var fromEmail = Environment.GetEnvironmentVariable("SMTP_FROM")
                         ?? Environment.GetEnvironmentVariable("EMAIL_FROM")
                         ?? "info@trailblazer-academy.com";
            var fromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME")
                        ?? "Trailblazers Academy";

            var payload = new Dictionary<string, object>
            {
                ["sender"] = new { name = fromName, email = fromEmail },
                ["to"] = new[] { new { email = to } },
                ["subject"] = subject
            };

            if (isHtml)
            {
                payload["htmlContent"] = body;
            }
            else
            {
                payload["textContent"] = body;
            }

            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.brevo.com/v3/smtp/email", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("[Brevo API Error] Status: {StatusCode}, Details: {Details}", response.StatusCode, errorBody);
                throw new HttpRequestException($"Brevo email delivery failed (HTTP {(int)response.StatusCode}): {errorBody}");
            }

            logger.LogInformation("Email successfully sent to {Recipient} via Brevo HTTP API (Port 443)", to);
        }

        private async Task SendViaSmtpAsync(string to, string subject, string body, bool isHtml)
        {
            var host = Environment.GetEnvironmentVariable("SMTP_HOST");
            var portStr = Environment.GetEnvironmentVariable("SMTP_PORT");
            var username = Environment.GetEnvironmentVariable("SMTP_USERNAME");
            var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
            var from = Environment.GetEnvironmentVariable("SMTP_FROM")
                    ?? Environment.GetEnvironmentVariable("EMAIL_FROM")
                    ?? "noreply@trailblazers.com";
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            bool isDev = string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(host) || (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) && !isDev))
            {
                var msg = "No email provider is configured in environment variables. Render blocks outbound SMTP ports (25, 465, 587) by default. For seamless email on Render, set RESEND_API_KEY in your Render Environment Variables (free at https://resend.com), or set SMTP_HOST, SMTP_PORT, SMTP_USERNAME, and SMTP_PASSWORD.";
                logger.LogWarning("{Message}", msg);
                throw new InvalidOperationException(msg);
            }

            int port = 587;
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
            client.Timeout = 6000;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));

            try
            {
                var allowInvalidCert = string.Equals(
                    Environment.GetEnvironmentVariable("SMTP_ALLOW_INVALID_CERT"),
                    "true",
                    StringComparison.OrdinalIgnoreCase);

                if (allowInvalidCert)
                {
                    logger.LogWarning("SECURITY WARNING: SMTP TLS certificate validation is disabled via SMTP_ALLOW_INVALID_CERT=true.");
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                }

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
                logger.LogInformation("Email successfully sent to {Recipient} via SMTP ({Host}:{Port})", to, host, port);
            }
            catch (Exception ex) when (ex is TimeoutException or OperationCanceledException)
            {
                logger.LogError("[SMTP Timeout] Timed out connecting to '{Host}:{Port}'. NOTE: Cloud hosting providers like Render block outbound SMTP ports (25, 465, 587). Use RESEND_API_KEY to send over HTTPS (port 443).", host, port);
                throw new TimeoutException($"SMTP connection to {host}:{port} timed out after 6 seconds. Render blocks outbound SMTP ports (25, 465, 587). Please set RESEND_API_KEY in your Render dashboard to send emails via HTTPS.", ex);
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
