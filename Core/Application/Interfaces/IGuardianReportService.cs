using System;
using System.Threading;
using System.Threading.Tasks;
using Trailblazers.Backend.Core.Application.Features.GuardianReports.Dtos;

namespace Trailblazers.Backend.Core.Application.Interfaces
{
    public interface IGuardianReportService
    {
        Task<GuardianReportPreviewDto> GetReportPreviewAsync(
            string studentEmail,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken = default);

        Task<SendGuardianReportResponseDto> SendReportAsync(
            SendGuardianReportRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
