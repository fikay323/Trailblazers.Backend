using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trailblazers.Backend.Core.Application.Common.Commands;
using Trailblazers.Backend.Core.Application.Features.Announcements.Dtos;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Infrastructure.Persistence;

namespace Trailblazers.Backend.Infrastructure.Services
{
    public class AnnouncementService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IEmailTemplateService templateService,
        IBackgroundTaskQueue taskQueue,
        ILogger<AnnouncementService> logger) : IAnnouncementService
    {
        public async Task<List<AnnouncementDto>> GetActiveAnnouncementsAsync(
            string? targetAudience = null,
            CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            var query = dbContext.Announcements
                .AsNoTracking()
                .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > now));

            if (!string.IsNullOrWhiteSpace(targetAudience) && !targetAudience.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.TargetAudience == "All" || a.TargetAudience == targetAudience);
            }

            var items = await query
                .OrderByDescending(a => a.Priority)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync(cancellationToken);

            return items.Select(MapToDto).ToList();
        }

        public async Task<AnnouncementsListResponseDto> GetAllAnnouncementsAsync(
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;

            var query = dbContext.Announcements
                .AsNoTracking();

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new AnnouncementsListResponseDto
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = items.Select(MapToDto).ToList()
            };
        }

        public async Task<AnnouncementDto> CreateAnnouncementAsync(
            CreateAnnouncementRequestDto request,
            string authorName,
            Guid? authorId,
            CancellationToken cancellationToken = default)
        {
            var announcement = new Announcement
            {
                Id = Guid.NewGuid(),
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                Priority = request.Priority,
                TargetAudience = string.IsNullOrWhiteSpace(request.TargetAudience) ? "All" : request.TargetAudience.Trim(),
                ExpiresAt = request.ExpiresAt,
                AuthorName = string.IsNullOrWhiteSpace(authorName) ? "Academy Administration" : authorName.Trim(),
                AuthorId = authorId,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                SentEmailBroadcast = request.SendEmailBroadcast,
                SentSmsBroadcast = request.SendSmsBroadcast
            };

            dbContext.Announcements.Add(announcement);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Trigger Email Broadcast in background if requested
            if (request.SendEmailBroadcast)
            {
                try
                {
                    var students = await userManager.GetUsersInRoleAsync("Student");
                    var portalUrl = "https://learn.trailblazer-academy.com/student/dashboard";

                    foreach (var student in students.Where(s => s.IsActive && !string.IsNullOrWhiteSpace(s.Email)))
                    {
                        var emailBody = templateService.RenderAnnouncementBroadcastEmail(
                            recipientName: student.FullName,
                            title: announcement.Title,
                            content: announcement.Content,
                            priorityName: announcement.Priority.ToString(),
                            authorName: announcement.AuthorName,
                            publishedAt: announcement.CreatedAt,
                            portalUrl: portalUrl);

                        var emailCommand = new SendEmailCommand(
                            To: student.Email!,
                            Subject: $"[{announcement.Priority.ToString().ToUpperInvariant()}] {announcement.Title} - Trailblazers Academy",
                            Body: emailBody,
                            IsHtml: true);

                        await taskQueue.QueueBackgroundWorkItemAsync(emailCommand);
                    }

                    logger.LogInformation("Enqueued announcement broadcast email for {Count} active students", students.Count);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to enqueue email broadcasts for announcement {Id}", announcement.Id);
                }
            }

            return MapToDto(announcement);
        }

        public async Task<AnnouncementDto?> UpdateAnnouncementAsync(
            Guid id,
            UpdateAnnouncementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var announcement = await dbContext.Announcements.FindAsync([id], cancellationToken);
            if (announcement == null) return null;

            if (!string.IsNullOrWhiteSpace(request.Title)) announcement.Title = request.Title.Trim();
            if (!string.IsNullOrWhiteSpace(request.Content)) announcement.Content = request.Content.Trim();
            if (request.Priority.HasValue) announcement.Priority = request.Priority.Value;
            if (!string.IsNullOrWhiteSpace(request.TargetAudience)) announcement.TargetAudience = request.TargetAudience.Trim();
            if (request.IsActive.HasValue) announcement.IsActive = request.IsActive.Value;
            if (request.ExpiresAt.HasValue) announcement.ExpiresAt = request.ExpiresAt.Value;

            await dbContext.SaveChangesAsync(cancellationToken);
            return MapToDto(announcement);
        }

        public async Task<bool> DeleteAnnouncementAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var announcement = await dbContext.Announcements.FindAsync([id], cancellationToken);
            if (announcement == null) return false;

            dbContext.Announcements.Remove(announcement);
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static AnnouncementDto MapToDto(Announcement a)
        {
            return new AnnouncementDto
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                Priority = a.Priority,
                TargetAudience = a.TargetAudience,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                ExpiresAt = a.ExpiresAt,
                AuthorName = a.AuthorName,
                SentEmailBroadcast = a.SentEmailBroadcast,
                SentSmsBroadcast = a.SentSmsBroadcast
            };
        }
    }
}
