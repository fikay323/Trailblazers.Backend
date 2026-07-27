using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trailblazers.Backend.Core.Application.Features.Exams.SeedQuestions;
using Trailblazers.Backend.Core.Application.Interfaces;

namespace Trailblazers.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController(IMediator mediator, IBackgroundTaskQueue taskQueue, ILogger<AdminController> logger)
        : ControllerBase
    {
        [HttpPost("seed-jamb-data")]
        public async Task<IActionResult> SeedJambData(
            [FromBody] SeedJambDataRequest request,
            CancellationToken cancellationToken)
        {
            logger.LogInformation(
                "Admin HTTP Request received: POST /api/admin/seed-jamb-data. Subject: {Subject}, Year: {Year}",
                request.Subject, request.Year);

            try
            {
                if (string.IsNullOrWhiteSpace(request.Subject))
                {
                    logger.LogWarning("Validation failed: Subject is required.");
                    return BadRequest(new { error = "Subject is required." });
                }

                if (request.Year < 1980 || request.Year > 2026)
                {
                    logger.LogWarning("Validation failed: Year {Year} is out of bounds.", request.Year);
                    return BadRequest(new { error = "Please specify a valid exam year." });
                }

                var command = new SeedQuestionsCommand(request.Subject, request.Year, request.Amount);
                var insertedCount = await mediator.Send(command, cancellationToken);

                logger.LogInformation("Successfully completed seeding operation. Count: {Count}", insertedCount);

                return Ok(new
                {
                    message = $"Successfully seeded {insertedCount} questions.",
                    count = insertedCount
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unhandled exception occurred while processing the admin seeding request.");
                return StatusCode(500, new { error = "Failed to complete seeding task.", details = ex.Message });
            }
        }

        [HttpPost("seed-all-jamb-data")]
        public async Task<IActionResult> SeedAllJambData()
        {
            logger.LogInformation(
                "Admin HTTP Request received: POST /api/admin/seed-all-jamb-data. Enqueuing bulk background seeding job...");

            await taskQueue.QueueBackgroundWorkItemAsync(new SeedAllQuestionsCommand());

            return Accepted(new { message = "Bulk background JAMB seeding job successfully initiated." });
        }
    }

    public class SeedJambDataRequest
    {
        public string Subject { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Amount { get; set; } = 40;
    }
}
