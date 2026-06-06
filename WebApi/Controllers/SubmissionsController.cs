using Microsoft.AspNetCore.Mvc;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Core.Application.Submissions.Commands;
using Trailblazers.Backend.Core.Application.Submissions.Queries;

namespace Trailblazers.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubmissionsController(
        SubmitContactCommandHandler contactHandler,
        SubmitRegistrationCommandHandler registrationHandler,
        GetSubmissionsQueryHandler queryHandler)
        : ControllerBase
    {
        [HttpPost("contact")]
        public async Task<IActionResult> SubmitContact(
            [FromBody] SubmitContactCommand command,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await contactHandler.HandleAsync(command, cancellationToken);
                return CreatedAtAction(nameof(SubmitContact), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An internal server error occurred.", details = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> SubmitRegistration(
            [FromBody] SubmitRegistrationCommand command,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await registrationHandler.HandleAsync(command, cancellationToken);
                return CreatedAtAction(nameof(SubmitRegistration), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An internal server error occurred.", details = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSubmissions(
            [FromQuery] SubmissionType? type,
            [FromQuery] string? searchTerm,
            [FromQuery] DateTimeOffset? startDate,
            [FromQuery] DateTimeOffset? endDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var expectedApiKey = Environment.GetEnvironmentVariable("ADMIN_API_KEY") ?? "trailblazers-secret-key";
            if (!Request.Headers.TryGetValue("X-API-KEY", out var extractedApiKey) || extractedApiKey != expectedApiKey)
            {
                return Unauthorized(new
                    { error = "Unauthorized access to submissions. A valid X-API-KEY header is required." });
            }

            try
            {
                var query = new GetSubmissionsQuery(
                    Type: type,
                    SearchTerm: searchTerm,
                    StartDate: startDate,
                    EndDate: endDate,
                    PageNumber: pageNumber,
                    PageSize: pageSize
                );

                var result = await queryHandler.HandleAsync(query, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An internal error occurred.", details = ex.Message });
            }
        }
    }
}
