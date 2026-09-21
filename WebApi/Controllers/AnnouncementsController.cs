using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Trailblazers.Backend.Core.Application.Features.Announcements.Dtos;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.WebApi.Authentication;

namespace Trailblazers.Backend.WebApi.Controllers
{
    [ApiController]
    public class AnnouncementsController(
        IAnnouncementService announcementService,
        ILogger<AnnouncementsController> logger) : ControllerBase
    {
        // Public & Student endpoint: Active notices
        [HttpGet("api/announcements")]
        public async Task<IActionResult> GetActiveAnnouncements(
            [FromQuery] string? targetAudience,
            CancellationToken cancellationToken)
        {
            try
            {
                var announcements = await announcementService.GetActiveAnnouncementsAsync(targetAudience, cancellationToken);
                return Ok(announcements);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve active announcements");
                return StatusCode(500, new { error = "Failed to retrieve active announcements." });
            }
        }

        // Admin: List all announcements with pagination
        [HttpGet("api/admin/announcements")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> GetAllAnnouncements(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await announcementService.GetAllAnnouncementsAsync(pageNumber, pageSize, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve admin announcements");
                return StatusCode(500, new { error = "Failed to retrieve announcements." });
            }
        }

        // Admin: Create notice
        [HttpPost("api/admin/announcements")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> CreateAnnouncement(
            [FromBody] CreateAnnouncementRequestDto request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { error = "Title and Content are required." });
            }

            string authorName = "Academy Administration";
            Guid? authorId = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                authorName = User.Identity.Name ?? authorName;
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(idClaim, out var parsedId)) authorId = parsedId;
            }

            try
            {
                var created = await announcementService.CreateAnnouncementAsync(request, authorName, authorId, cancellationToken);
                return CreatedAtAction(nameof(GetActiveAnnouncements), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create announcement");
                return StatusCode(500, new { error = "Failed to create announcement." });
            }
        }

        // Admin: Update notice
        [HttpPut("api/admin/announcements/{id:guid}")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> UpdateAnnouncement(
            Guid id,
            [FromBody] UpdateAnnouncementRequestDto request,
            CancellationToken cancellationToken)
        {
            try
            {
                var updated = await announcementService.UpdateAnnouncementAsync(id, request, cancellationToken);
                if (updated == null)
                {
                    return NotFound(new { error = "Announcement not found." });
                }

                return Ok(updated);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update announcement {Id}", id);
                return StatusCode(500, new { error = "Failed to update announcement." });
            }
        }

        // Admin: Delete notice
        [HttpDelete("api/admin/announcements/{id:guid}")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> DeleteAnnouncement(
            Guid id,
            CancellationToken cancellationToken)
        {
            try
            {
                var deleted = await announcementService.DeleteAnnouncementAsync(id, cancellationToken);
                if (!deleted)
                {
                    return NotFound(new { error = "Announcement not found." });
                }

                return Ok(new { message = "Announcement deleted successfully." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete announcement {Id}", id);
                return StatusCode(500, new { error = "Failed to delete announcement." });
            }
        }
    }
}
