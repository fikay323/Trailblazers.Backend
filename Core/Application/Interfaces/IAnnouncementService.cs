using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trailblazers.Backend.Core.Application.Features.Announcements.Dtos;

namespace Trailblazers.Backend.Core.Application.Interfaces
{
    public interface IAnnouncementService
    {
        Task<List<AnnouncementDto>> GetActiveAnnouncementsAsync(
            string? targetAudience = null,
            CancellationToken cancellationToken = default);

        Task<AnnouncementsListResponseDto> GetAllAnnouncementsAsync(
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        Task<AnnouncementDto> CreateAnnouncementAsync(
            CreateAnnouncementRequestDto request,
            string authorName,
            Guid? authorId,
            CancellationToken cancellationToken = default);

        Task<AnnouncementDto?> UpdateAnnouncementAsync(
            Guid id,
            UpdateAnnouncementRequestDto request,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAnnouncementAsync(
            Guid id,
            CancellationToken cancellationToken = default);
    }
}
