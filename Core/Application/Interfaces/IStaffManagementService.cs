using Trailblazers.Backend.Core.Application.Features.Staff.Dtos;

namespace Trailblazers.Backend.Core.Application.Interfaces
{
    public interface IStaffManagementService
    {
        Task<List<StaffMemberDto>> GetStaffRosterAsync(CancellationToken cancellationToken = default);

        Task<(bool Succeeded, string? Error, StaffMemberDto? StaffMember)> UpdateStaffRoleAsync(
            Guid staffId,
            string newRole,
            Guid requestedByUserId,
            CancellationToken cancellationToken = default);

        Task<(bool Succeeded, string? Error)> ToggleStaffStatusAsync(
            Guid staffId,
            bool isActive,
            string? reason,
            Guid requestedByUserId,
            CancellationToken cancellationToken = default);
    }
}
