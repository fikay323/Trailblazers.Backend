using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trailblazers.Backend.Core.Application.Features.Attendance.Dtos;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.WebApi.Authentication;

namespace Trailblazers.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/attendance")]
    public class AttendanceController(
        IAttendanceService attendanceService,
        ILogger<AttendanceController> logger) : ControllerBase
    {
        // 1. Student Clock-In with GPS coordinates
        [HttpPost("clock-in")]
        [Authorize]
        public async Task<IActionResult> ClockIn([FromBody] ClockInRequestDto request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? User.FindFirstValue("sub");

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var studentId))
            {
                return Unauthorized(new { error = "Valid authenticated student session required." });
            }

            try
            {
                var record = await attendanceService.ClockInAsync(studentId, request);
                return Ok(record);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during attendance clock-in for student {Id}", studentId);
                return StatusCode(500, new { error = "An error occurred while registering attendance. Please try again." });
            }
        }

        // 2. Student Check-In Status Today & History Stats
        [HttpGet("my-today")]
        [Authorize]
        public async Task<IActionResult> GetMyTodayStatus()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? User.FindFirstValue("sub");

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var studentId))
            {
                return Unauthorized(new { error = "Valid authenticated student session required." });
            }

            var stats = await attendanceService.GetStudentTodayStatusAsync(studentId);
            return Ok(stats);
        }

        // 3. Staff & Admin: Get Daily Roster
        [HttpGet("roster")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> GetDailyRoster([FromQuery] string? date)
        {
            DateOnly queryDate;
            if (string.IsNullOrWhiteSpace(date) || !DateOnly.TryParse(date, out queryDate))
            {
                // Default to today in West Africa Time (UTC+1)
                var localNow = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(1));
                queryDate = DateOnly.FromDateTime(localNow.DateTime);
            }

            var roster = await attendanceService.GetDailyRosterAsync(queryDate);
            return Ok(roster);
        }

        // 4. Staff & Admin: Override Student Status (Mark Present, Unmark Absent, Excused)
        [HttpPost("override")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> OverrideAttendance([FromBody] AttendanceOverrideRequestDto request)
        {
            var staffUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var staffUserId = Guid.TryParse(staffUserIdStr, out var sId) ? sId : Guid.Empty;
            var staffUserName = User.FindFirstValue(ClaimTypes.Name)
                                ?? User.FindFirstValue("unique_name")
                                ?? "Staff Member";

            try
            {
                var record = await attendanceService.OverrideAttendanceAsync(staffUserId, staffUserName, request);
                return Ok(record);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to override attendance for student {Id}", request.StudentId);
                return BadRequest(new { error = ex.Message });
            }
        }

        // 5. Staff & Admin: Get Geofence & Schedule Settings
        [HttpGet("settings")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await attendanceService.GetSettingsAsync();
            return Ok(settings);
        }

        // 6. Admin: Update Geofence & Schedule Settings
        [HttpPut("settings")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> UpdateSettings([FromBody] AttendanceSettingDto request)
        {
            var adminName = User.FindFirstValue(ClaimTypes.Name)
                            ?? User.FindFirstValue("unique_name")
                            ?? "Academy Admin";

            try
            {
                var updated = await attendanceService.UpdateSettingsAsync(adminName, request);
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
