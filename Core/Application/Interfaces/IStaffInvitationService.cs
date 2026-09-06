using Trailblazers.Backend.Core.Application.Features.Staff.Dtos;

namespace Trailblazers.Backend.Core.Application.Interfaces
{
    public interface IStaffInvitationService
    {
        Task<StaffInvitationDto> InviteStaffAsync(
            InviteStaffRequestDto request,
            Guid invitedByUserId,
            string invitedByUserName,
            CancellationToken cancellationToken = default);

        Task<ValidateInvitationResponseDto> ValidateInvitationAsync(
            string token,
            string email,
            CancellationToken cancellationToken = default);

        Task<(bool Succeeded, string? Error, StaffAuthResultDto? AuthResult)> AcceptInvitationAsync(
            AcceptInvitationRequestDto request,
            CancellationToken cancellationToken = default);

        Task<bool> ResendInvitationAsync(
            Guid invitationId,
            Guid requestedByUserId,
            CancellationToken cancellationToken = default);
    }

    public class StaffAuthResultDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
