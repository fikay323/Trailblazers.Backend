using System.Threading;
using System.Threading.Tasks;
using Trailblazers.Backend.Core.Application.Features.GuardianPortal.Dtos;

namespace Trailblazers.Backend.Core.Application.Features.GuardianPortal.Interfaces
{
    public interface IGuardianPortalService
    {
        Task<GuardianAccessResponseDto> VerifyAccessAsync(
            GuardianAccessRequestDto request,
            CancellationToken cancellationToken = default);

        Task<GuardianWardOverviewDto> GetWardOverviewAsync(
            string accessToken,
            CancellationToken cancellationToken = default);

        Task<GuardianInquiryResponseDto> SubmitInquiryAsync(
            GuardianInquiryRequestDto request,
            CancellationToken cancellationToken = default);

        string GenerateAccessToken(string studentEmail);

        bool TryValidateAccessToken(string accessToken, out string studentEmail);
    }
}
