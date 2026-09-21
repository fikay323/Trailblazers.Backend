using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Core.Domain.Repositories;
using Trailblazers.Backend.Core.Application.Submissions.Commands;
using Trailblazers.Backend.Core.Application.Submissions.Queries;
using Trailblazers.Backend.WebApi.Authentication;

namespace Trailblazers.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubmissionsController(
        SubmitContactCommandHandler contactHandler,
        SubmitRegistrationCommandHandler registrationHandler,
        GetSubmissionsQueryHandler queryHandler,
        ISubmissionRepository repository)
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
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> GetSubmissions(
            [FromQuery] SubmissionType? type,
            [FromQuery] string? searchTerm,
            [FromQuery] DateTimeOffset? startDate,
            [FromQuery] DateTimeOffset? endDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
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

        [HttpDelete("{id:guid}")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> DeleteSubmission(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var deleted = await repository.DeleteAsync(id, cancellationToken);
                if (!deleted)
                {
                    return NotFound(new { error = "Submission not found." });
                }

                return Ok(new { message = "Submission deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An internal error occurred while deleting submission.", details = ex.Message });
            }
        }

        [HttpPost("{id:guid}/create-student-account")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> CreateStudentAccount(
            Guid id,
            [FromServices] IStaffInvitationService invitationService,
            CancellationToken cancellationToken)
        {
            try
            {
                var submission = await repository.GetByIdAsync(id, cancellationToken);
                if (submission == null)
                {
                    return NotFound(new { error = "Registration submission not found." });
                }

                if (submission.Type != SubmissionType.Registration)
                {
                    return BadRequest(new { error = "Accounts can only be created for registration submissions." });
                }

                // Parse metadata to extract target exam
                string targetExam = "JAMB / WAEC";
                if (!string.IsNullOrWhiteSpace(submission.Metadata))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(submission.Metadata);
                        if (doc.RootElement.TryGetProperty("TargetExam", out var examProp))
                        {
                            targetExam = examProp.GetString() ?? targetExam;
                        }
                    }
                    catch
                    {
                        // Fallback to default
                    }
                }

                // Determine inviter info from current user or system admin
                Guid inviterId = Guid.Empty;
                string inviterName = "Administrator";
                if (User.Identity?.IsAuthenticated == true)
                {
                    var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (Guid.TryParse(idClaim, out var parsedId)) inviterId = parsedId;
                    inviterName = User.Identity.Name ?? "Administrator";
                }

                var inviteResult = await invitationService.InviteStudentAsync(
                    email: submission.Email,
                    fullName: submission.Name,
                    targetExam: targetExam,
                    invitedByUserId: inviterId,
                    invitedByUserName: inviterName,
                    cancellationToken: cancellationToken);

                // Update submission metadata with AccountCreated = true
                var metadataDict = new Dictionary<string, object>();
                if (!string.IsNullOrWhiteSpace(submission.Metadata))
                {
                    try
                    {
                        metadataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(submission.Metadata) ?? [];
                    }
                    catch
                    {
                        metadataDict = [];
                    }
                }
                metadataDict["AccountCreated"] = true;
                metadataDict["AccountCreatedAt"] = DateTimeOffset.UtcNow;
                metadataDict["AccountInviteUrl"] = inviteResult.InviteUrl ?? string.Empty;
                metadataDict["AccountEmailSent"] = inviteResult.EmailSent;

                submission.Metadata = JsonSerializer.Serialize(metadataDict);
                await repository.SaveChangesAsync(cancellationToken);

                return Ok(new
                {
                    message = "Student account invitation generated successfully.",
                    invitation = inviteResult
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An internal error occurred while creating student account.", details = ex.Message });
            }
        }
    }
}
