using Trailblazers.Backend.Core.Application.Features.Attendance.Dtos;

namespace Trailblazers.Backend.Core.Application.Interfaces
{
    public interface IAttendanceService
    {
        Task<AttendanceRecordDto> ClockInAsync(Guid studentId, ClockInRequestDto request);
        Task<StudentAttendanceStatsDto> GetStudentTodayStatusAsync(Guid studentId);
        Task<DailyRosterResponseDto> GetDailyRosterAsync(DateOnly date);
        Task<AttendanceRecordDto> OverrideAttendanceAsync(Guid staffUserId, string staffUserName, AttendanceOverrideRequestDto request);
        Task<AttendanceSettingDto> GetSettingsAsync();
        Task<AttendanceSettingDto> UpdateSettingsAsync(string updatedBy, AttendanceSettingDto request);
    }
}
