using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Infrastructure.Persistence;
using Trailblazers.Backend.WebApi.Authentication;

namespace Trailblazers.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/admin/students")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public class AdminStudentsController(
        UserManager<ApplicationUser> userManager,
        IExamSessionRepository sessionRepository,
        IImageStorageService imageStorageService,
        ApplicationDbContext dbContext,
        IMailService mailService,
        IEmailTemplateService emailTemplateService,
        IConfiguration configuration,
        ILogger<AdminStudentsController> logger) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAllStudents(CancellationToken cancellationToken)
        {
            // 1. Gather all users and staff in the system
            var allUsers = await userManager.Users.ToListAsync(cancellationToken);
            var adminUsers = await userManager.GetUsersInRoleAsync("Admin");
            var instructorUsers = await userManager.GetUsersInRoleAsync("Instructor");
            var staffUserIds = adminUsers.Select(u => u.Id).Concat(instructorUsers.Select(u => u.Id)).ToHashSet();
            var staffEmails = new HashSet<string>(
                adminUsers.Concat(instructorUsers).Select(u => (u.Email ?? string.Empty).Trim().ToLowerInvariant()),
                StringComparer.OrdinalIgnoreCase);

            // 2. Identify students currently in "Student" role
            var students = (await userManager.GetUsersInRoleAsync("Student")).ToList();
            var studentUserIds = students.Select(s => s.Id).ToHashSet();
            var studentEmails = new HashSet<string>(
                students.Select(s => (s.Email ?? string.Empty).Trim().ToLowerInvariant()),
                StringComparer.OrdinalIgnoreCase);

            // Auto-heal any existing ApplicationUsers who are neither Admin nor Instructor: they are students!
            foreach (var user in allUsers)
            {
                if (!staffUserIds.Contains(user.Id) && !studentUserIds.Contains(user.Id))
                {
                    var cleanEmail = (user.Email ?? string.Empty).Trim().ToLowerInvariant();
                    await userManager.AddToRoleAsync(user, "Student");
                    students.Add(user);
                    studentUserIds.Add(user.Id);
                    if (!string.IsNullOrWhiteSpace(cleanEmail))
                    {
                        studentEmails.Add(cleanEmail);
                    }
                    logger.LogInformation("Assigned missing 'Student' role to existing user {Email} ({Name})", user.Email, user.FullName);
                }
            }

            // 3. Inspect StaffInvitations (case-insensitive for 'Student')
            var studentInvites = await dbContext.StaffInvitations
                .Where(x => x.Role.ToLower() == "student")
                .ToListAsync(cancellationToken);

            var invitesByEmail = studentInvites
                .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                .GroupBy(x => x.Email.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

            // 4. Inspect Submissions (find all registrations and anyone enrolled)
            var registrationSubmissions = await dbContext.Submissions
                .Where(s => s.Type == SubmissionType.Registration)
                .ToListAsync(cancellationToken);

            var submissionsByEmail = registrationSubmissions
                .Where(s => !string.IsNullOrWhiteSpace(s.Email))
                .GroupBy(s => s.Email.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.CreatedAt).First());

            // Sync from student invitations
            foreach (var invite in studentInvites)
            {
                var cleanEmail = (invite.Email ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(cleanEmail) || studentEmails.Contains(cleanEmail))
                {
                    continue;
                }

                var existingUser = await userManager.FindByEmailAsync(cleanEmail);
                if (existingUser != null)
                {
                    if (!staffUserIds.Contains(existingUser.Id))
                    {
                        var roles = await userManager.GetRolesAsync(existingUser);
                        if (!roles.Contains("Student"))
                        {
                            await userManager.AddToRoleAsync(existingUser, "Student");
                        }
                        students.Add(existingUser);
                        studentEmails.Add(cleanEmail);
                    }
                }
                else
                {
                    submissionsByEmail.TryGetValue(cleanEmail, out var sub);
                    string? phone = null;
                    if (!string.IsNullOrWhiteSpace(sub?.Metadata))
                    {
                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(sub.Metadata);
                            if (doc.RootElement.TryGetProperty("PhoneNumber", out var pProp))
                            {
                                phone = pProp.GetString();
                            }
                            else if (doc.RootElement.TryGetProperty("Phone", out var pProp2))
                            {
                                phone = pProp2.GetString();
                            }
                        }
                        catch { }
                    }

                    var newUser = new ApplicationUser
                    {
                        UserName = cleanEmail,
                        Email = cleanEmail,
                        FullName = invite.FullName,
                        PhoneNumber = phone,
                        EmailConfirmed = invite.IsAccepted,
                        IsActive = true,
                        CreatedAt = invite.CreatedAt
                    };

                    var tempPassword = Guid.NewGuid().ToString("N") + "!Aa1";
                    var createRes = await userManager.CreateAsync(newUser, tempPassword);
                    if (createRes.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newUser, "Student");
                        students.Add(newUser);
                        studentEmails.Add(cleanEmail);
                        logger.LogInformation("Provisioned student ApplicationUser from invitation for {Email} ({Name})",
                            cleanEmail, invite.FullName);
                    }
                }
            }

            // Sync from registrations where AccountCreated is true
            foreach (var (subEmail, sub) in submissionsByEmail)
            {
                if (studentEmails.Contains(subEmail) || staffEmails.Contains(subEmail)) continue;

                bool accountCreated = false;
                string? phone = null;
                if (!string.IsNullOrWhiteSpace(sub.Metadata))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(sub.Metadata);
                        if (doc.RootElement.TryGetProperty("AccountCreated", out var acProp))
                        {
                            accountCreated = acProp.ValueKind == System.Text.Json.JsonValueKind.True ||
                                             (acProp.ValueKind == System.Text.Json.JsonValueKind.String &&
                                              string.Equals(acProp.GetString(), "true", StringComparison.OrdinalIgnoreCase));
                        }
                        if (doc.RootElement.TryGetProperty("PhoneNumber", out var pProp))
                        {
                            phone = pProp.GetString();
                        }
                        else if (doc.RootElement.TryGetProperty("Phone", out var pProp2))
                        {
                            phone = pProp2.GetString();
                        }
                    }
                    catch { }
                }

                if (accountCreated)
                {
                    var existingUser = await userManager.FindByEmailAsync(subEmail);
                    if (existingUser != null)
                    {
                        if (!staffUserIds.Contains(existingUser.Id))
                        {
                            var roles = await userManager.GetRolesAsync(existingUser);
                            if (!roles.Contains("Student"))
                            {
                                await userManager.AddToRoleAsync(existingUser, "Student");
                            }
                            students.Add(existingUser);
                            studentEmails.Add(subEmail);
                        }
                    }
                    else
                    {
                        var newUser = new ApplicationUser
                        {
                            UserName = subEmail,
                            Email = subEmail,
                            FullName = sub.Name,
                            PhoneNumber = phone,
                            EmailConfirmed = false,
                            IsActive = true,
                            CreatedAt = sub.CreatedAt
                        };

                        var tempPassword = Guid.NewGuid().ToString("N") + "!Aa1";
                        var createRes = await userManager.CreateAsync(newUser, tempPassword);
                        if (createRes.Succeeded)
                        {
                            await userManager.AddToRoleAsync(newUser, "Student");
                            students.Add(newUser);
                            studentEmails.Add(subEmail);
                            logger.LogInformation("Provisioned student ApplicationUser from registration for {Email} ({Name})",
                                subEmail, sub.Name);
                        }
                    }
                }
            }

            var allResults = (await sessionRepository.GetAllResultsAsync(cancellationToken)).ToList();

            var studentDtos = students.Select(s =>
            {
                var sEmail = (s.Email ?? string.Empty).Trim().ToLowerInvariant();
                var studentResults = allResults.Where(r => r.CandidateId == sEmail).ToList();
                var testsCount = studentResults.Count;

                double avgScore = 0;
                if (testsCount > 0)
                {
                    avgScore = Math.Round(studentResults.Average(r =>
                    {
                        var totalQ = r.Session?.AssignedQuestionIds?.Count ?? 50;
                        return totalQ > 0 ? (double)r.TotalScore / totalQ * 100 : 0;
                    }), 1);
                }

                invitesByEmail.TryGetValue(sEmail, out var invite);
                submissionsByEmail.TryGetValue(sEmail, out var sub);

                string? inviteUrl = null;
                if (!string.IsNullOrWhiteSpace(sub?.Metadata))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(sub.Metadata);
                        if (doc.RootElement.TryGetProperty("AccountInviteUrl", out var urlProp))
                        {
                            inviteUrl = urlProp.GetString();
                        }
                    }
                    catch { }
                }

                // An account is ONLY "Pending Activation" if there is an active, unaccepted, unexpired student invitation,
                // and the student has not completed any exams yet.
                // All other students (including all previous students and active test takers) are Active.
                bool isPendingActivation = invite != null && !invite.IsAccepted && invite.ExpiresAt > DateTimeOffset.UtcNow && testsCount == 0;
                bool isActivated = !isPendingActivation;

                // Auto-heal existing active students whose EmailConfirmed flag was left false in Identity
                if (isActivated && !s.EmailConfirmed)
                {
                    s.EmailConfirmed = true;
                    _ = userManager.UpdateAsync(s);
                }

                string status;
                if (!s.IsActive)
                {
                    status = "Suspended";
                }
                else if (isPendingActivation)
                {
                    status = "Pending Activation";
                }
                else
                {
                    status = "Active";
                }

                return new AdminStudentListItemDto
                {
                    Id = s.Id,
                    FullName = s.FullName,
                    Email = s.Email ?? string.Empty,
                    PhoneNumber = s.PhoneNumber,
                    IsActive = s.IsActive,
                    Status = status,
                    IsActivated = isActivated,
                    IsPendingActivation = isPendingActivation,
                    InviteUrl = inviteUrl,
                    DisabledReason = s.DisabledReason,
                    ProfilePictureUrl = s.ProfilePictureUrl,
                    TotalTestsTaken = testsCount,
                    AverageScore = avgScore,
                    CreatedAt = s.CreatedAt
                };
            }).OrderByDescending(s => s.CreatedAt).ToList();

            return Ok(studentDtos);
        }

        [HttpPost("{id:guid}/status")]
        public async Task<IActionResult> ToggleStudentStatus(
            Guid id,
            [FromBody] UpdateStudentStatusRequestDto request)
        {
            if (User.Identity?.IsAuthenticated == true && !User.IsInRole("Admin"))
            {
                return StatusCode(403, new { error = "Access denied: Only Administrators can modify student account fee status." });
            }

            var user = await userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound(new { error = "Student not found." });
            }

            user.IsActive = request.IsActive;
            user.DisabledReason = request.IsActive ? null : (request.Reason?.Trim() ?? "Account deactivated by administration.");

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                return BadRequest(new { error = errors });
            }

            logger.LogInformation("Admin updated student {Email} status: IsActive={IsActive}, Reason={Reason}",
                user.Email, user.IsActive, user.DisabledReason);

            return Ok(new
            {
                message = user.IsActive ? "Student account activated successfully." : "Student account deactivated.",
                user.Id,
                user.Email,
                user.IsActive,
                user.DisabledReason
            });
        }

        [HttpGet("{email}/history")]
        public async Task<IActionResult> GetStudentHistory(string email, CancellationToken cancellationToken)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var results = (await sessionRepository.GetResultsByStudentEmailAsync(normalizedEmail, cancellationToken)).ToList();

            var attempts = results.Select(r =>
            {
                var totalQuestions = (r.Session?.AssignedQuestionIds != null && r.Session.AssignedQuestionIds.Count > 0)
                    ? r.Session.AssignedQuestionIds.Count
                    : (r.Session?.Answers?.Count ?? 0);

                var percentage = totalQuestions > 0
                    ? Math.Round((double)r.TotalScore / totalQuestions * 100, 1)
                    : 0;

                return new
                {
                    SessionId = r.SessionId,
                    TargetYear = r.Session?.TargetYear ?? 0,
                    TotalScore = r.TotalScore,
                    TotalQuestions = totalQuestions,
                    Percentage = percentage,
                    CompletedAt = r.CompletedAt,
                    StartTime = r.Session?.StartTime ?? r.CompletedAt
                };
            }).ToList();

            return Ok(new
            {
                StudentEmail = normalizedEmail,
                TotalTestsTaken = attempts.Count,
                AveragePercentage = attempts.Count > 0 ? Math.Round(attempts.Average(a => a.Percentage), 1) : 0,
                HighestPercentage = attempts.Count > 0 ? attempts.Max(a => a.Percentage) : 0,
                Attempts = attempts
            });
        }

        [HttpDelete("{id:guid}/profile-picture")]
        public async Task<IActionResult> DeleteStudentProfilePicture(
            Guid id,
            [FromBody] DeleteProfilePictureByAdminRequestDto? request = null,
            [FromQuery] string? reason = null,
            CancellationToken cancellationToken = default)
        {
            var student = await userManager.FindByIdAsync(id.ToString());
            if (student == null)
            {
                return NotFound(new { error = "Student not found." });
            }

            var effectiveReason = !string.IsNullOrWhiteSpace(request?.Reason)
                ? request.Reason.Trim()
                : (!string.IsNullOrWhiteSpace(reason)
                    ? reason.Trim()
                    : "Profile picture did not comply with academy standards.");

            // Clean up Cloudinary asset if present
            if (!string.IsNullOrWhiteSpace(student.ProfilePicturePublicId))
            {
                logger.LogInformation("Admin deleting Cloudinary asset {PublicId} for student {StudentId}",
                    student.ProfilePicturePublicId, student.Id);
                await imageStorageService.DeleteImageAsync(student.ProfilePicturePublicId, cancellationToken);
            }

            student.ProfilePictureUrl = null;
            student.ProfilePicturePublicId = null;

            var updateResult = await userManager.UpdateAsync(student);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = errors });
            }

            var actorName = User.FindFirstValue("fullName")
                            ?? User.FindFirstValue(ClaimTypes.Name)
                            ?? "Academy Administration";

            // 1. Create in-app student notification for this specific student
            var notification = new StudentNotification
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                StudentEmail = student.Email ?? string.Empty,
                Title = "Profile Picture Removed",
                Message = $"Your profile picture was removed by {actorName}. Reason: {effectiveReason}",
                Type = "ProfilePictureRemoved",
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = actorName
            };
            dbContext.StudentNotifications.Add(notification);

            // 2. Create high-priority Announcement for Noticeboard
            var noticeboardAnnouncement = new Announcement
            {
                Id = Guid.NewGuid(),
                Title = "Profile Picture Notice - Removal Action",
                Content = $"Official Notice for {student.FullName}: Your uploaded profile photograph was removed by {actorName}. Reason: {effectiveReason}. Please upload an appropriate passport photograph compliant with academy guidelines.",
                Priority = AnnouncementPriority.Urgent,
                TargetAudience = "All",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                AuthorName = actorName
            };
            dbContext.Announcements.Add(noticeboardAnnouncement);

            await dbContext.SaveChangesAsync(cancellationToken);

            // Dispatch notification email to student
            try
            {
                var portalUrl = configuration["Frontend:StudentPortalUrl"]
                                ?? "https://learn.trailblazer.com/student/dashboard";

                var emailHtml = emailTemplateService.RenderProfilePictureRemovedEmail(
                    student.FullName,
                    effectiveReason,
                    portalUrl);

                await mailService.SendEmailAsync(
                    student.Email!,
                    "Profile Picture Removal Notice - Trailblazers Academy",
                    emailHtml,
                    isHtml: true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send profile picture removal email to {Email}", student.Email);
            }

            logger.LogInformation("Admin/Staff successfully removed profile picture for student {Email} with reason: {Reason}",
                student.Email, reason);

            return Ok(new
            {
                message = "Student profile picture removed successfully and notification dispatched.",
                studentId = student.Id,
                reason
            });
        }
    }

    public class AdminStudentListItemDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } = "Active"; // "Active", "Pending Activation", "Suspended"
        public bool IsActivated { get; set; }
        public bool IsPendingActivation { get; set; }
        public string? InviteUrl { get; set; }
        public string? DisabledReason { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public int TotalTestsTaken { get; set; }
        public double AverageScore { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class UpdateStudentStatusRequestDto
    {
        public bool IsActive { get; set; }
        public string? Reason { get; set; }
    }

    public class DeleteProfilePictureByAdminRequestDto
    {
        public string? Reason { get; set; }
    }
}
