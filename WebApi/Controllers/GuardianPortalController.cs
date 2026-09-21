using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Trailblazers.Backend.Core.Application.Features.GuardianPortal.Dtos;
using Trailblazers.Backend.Core.Application.Features.GuardianPortal.Interfaces;

namespace Trailblazers.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/guardian")]
    public class GuardianPortalController(
        IGuardianPortalService portalService,
        ILogger<GuardianPortalController> logger) : ControllerBase
    {
        [HttpPost("access")]
        public async Task<IActionResult> VerifyAccess(
            [FromBody] GuardianAccessRequestDto request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.StudentEmail) || string.IsNullOrWhiteSpace(request.GuardianContact))
            {
                return BadRequest(new { error = "Student email and Guardian contact (phone or email) are required." });
            }

            var result = await portalService.VerifyAccessAsync(request, cancellationToken);
            if (!result.Success)
            {
                return BadRequest(new { error = result.Message });
            }

            return Ok(result);
        }

        [HttpGet("ward-overview")]
        public async Task<IActionResult> GetWardOverview(
            [FromQuery] string token,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { error = "Access token is required." });
            }

            try
            {
                var overview = await portalService.GetWardOverviewAsync(token, cancellationToken);
                return Ok(overview);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving ward overview for token");
                return StatusCode(500, new { error = "An unexpected error occurred while fetching ward information." });
            }
        }

        [HttpPost("inquiry")]
        public async Task<IActionResult> SubmitInquiry(
            [FromBody] GuardianInquiryRequestDto request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.StudentEmail) || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Student email and inquiry message are required." });
            }

            var result = await portalService.SubmitInquiryAsync(request, cancellationToken);
            if (!result.Success)
            {
                return BadRequest(new { error = result.Message });
            }

            return Ok(result);
        }
    }
}
