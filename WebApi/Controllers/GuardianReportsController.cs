using Microsoft.AspNetCore.Mvc;
using Trailblazers.Backend.Core.Application.Features.GuardianReports.Dtos;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.WebApi.Authentication;

namespace Trailblazers.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/admin/guardian-reports")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public class GuardianReportsController(
        IGuardianReportService reportService,
        ILogger<GuardianReportsController> logger) : ControllerBase
    {
        [HttpGet("preview")]
        public async Task<IActionResult> GetReportPreview(
            [FromQuery] string studentEmail,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(studentEmail))
            {
                return BadRequest(new { error = "Student email is required." });
            }

            DateTimeOffset sDate = DateTimeOffset.UtcNow.AddDays(-30);
            if (!string.IsNullOrWhiteSpace(startDate) && DateTimeOffset.TryParse(startDate, out var parsedStart))
            {
                sDate = parsedStart;
            }

            DateTimeOffset eDate = DateTimeOffset.UtcNow;
            if (!string.IsNullOrWhiteSpace(endDate) && DateTimeOffset.TryParse(endDate, out var parsedEnd))
            {
                eDate = parsedEnd;
            }

            try
            {
                var preview = await reportService.GetReportPreviewAsync(studentEmail, sDate, eDate, cancellationToken);
                return Ok(preview);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate report preview for {Email}", studentEmail);
                return StatusCode(500, new { error = "Failed to generate guardian report preview." });
            }
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendReport(
            [FromBody] SendGuardianReportRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.StudentEmail))
            {
                return BadRequest(new { error = "Invalid request. Student email is required." });
            }

            if (string.IsNullOrWhiteSpace(request.GuardianEmail) && string.IsNullOrWhiteSpace(request.GuardianPhone))
            {
                return BadRequest(new { error = "At least one contact method (Guardian Email or Phone) must be provided." });
            }

            try
            {
                var result = await reportService.SendReportAsync(request, cancellationToken);
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send guardian report for student {Email}", request.StudentEmail);
                return StatusCode(500, new { error = "An error occurred while sending guardian report." });
            }
        }
    }
}
