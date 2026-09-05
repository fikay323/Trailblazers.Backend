using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Trailblazers.Backend.WebApi.Authentication
{
    public class ApiKeyAuthFilter(IConfiguration configuration, ILogger<ApiKeyAuthFilter> logger) : IAsyncActionFilter
    {
        private const string ApiKeyHeaderName = "X-API-KEY";
        private const string ApiKeyConfigKey = "ADMIN_API_KEY";

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Allow authenticated users with Admin or Instructor role
            if (context.HttpContext.User.Identity?.IsAuthenticated == true &&
                (context.HttpContext.User.IsInRole("Admin") || context.HttpContext.User.IsInRole("Instructor")))
            {
                await next();
                return;
            }

            var expectedApiKey = Environment.GetEnvironmentVariable(ApiKeyConfigKey)
                                 ?? configuration[ApiKeyConfigKey]
                                 ?? "trailblazers-secret-key";

            if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey)
                || string.IsNullOrWhiteSpace(extractedApiKey))
            {
                logger.LogWarning("Access denied: Missing {HeaderName} or Admin authorization on endpoint {Path}.",
                    ApiKeyHeaderName, context.HttpContext.Request.Path);

                context.Result = new UnauthorizedObjectResult(new
                {
                    error = $"Unauthorized access. A valid {ApiKeyHeaderName} header or Admin authorization is required."
                });
                return;
            }

            var expectedBytes = Encoding.UTF8.GetBytes(expectedApiKey);
            var extractedBytes = Encoding.UTF8.GetBytes(extractedApiKey.ToString());

            // Use constant-time comparison to prevent timing attacks
            if (expectedBytes.Length != extractedBytes.Length ||
                !CryptographicOperations.FixedTimeEquals(expectedBytes, extractedBytes))
            {
                logger.LogWarning("Access denied: Invalid {HeaderName} provided on endpoint {Path}.",
                    ApiKeyHeaderName, context.HttpContext.Request.Path);

                context.Result = new UnauthorizedObjectResult(new
                {
                    error = $"Unauthorized access. The provided {ApiKeyHeaderName} is invalid."
                });
                return;
            }

            await next();
        }
    }
}
