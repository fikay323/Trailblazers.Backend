using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Trailblazers.Backend.Core.Application.Features.Attendance.Dtos;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Infrastructure.Persistence;

namespace Trailblazers.Backend.Infrastructure.Services
{
    public class AttendanceService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger<AttendanceService> logger) : IAttendanceService
    {
        // West Africa Time (WAT) UTC+1 offset for Lagos, Nigeria
        private static readonly TimeSpan LocalTimezoneOffset = TimeSpan.FromHours(1);

        public async Task<AttendanceRecordDto> ClockInAsync(Guid studentId, ClockInRequestDto request)
        {
            var student = await userManager.FindByIdAsync(studentId.ToString())
                ?? throw new KeyNotFoundException("Student record not found.");

            if (!student.IsActive)
            {
                throw new InvalidOperationException("Your student account is currently suspended. Please contact the administration.");
            }

            var settings = await GetOrCreateSettingsInternalAsync();

            // 1. Authenticity Validation: Physical GPS Bounds
            if (request.Latitude < -90.0 || request.Latitude > 90.0 ||
                request.Longitude < -180.0 || request.Longitude > 180.0 ||
                (Math.Abs(request.Latitude) < 0.0001 && Math.Abs(request.Longitude) < 0.0001))
            {
                throw new ArgumentException("Invalid GPS coordinate values detected.");
            }

            // 2. Authenticity Validation: Hardware GPS Accuracy Threshold
            if (request.AccuracyMeters <= 0 || request.AccuracyMeters > settings.MaxAllowedAccuracyMeters)
            {
                throw new InvalidOperationException(
                    $"GPS accuracy ({Math.Round(request.AccuracyMeters)}m) exceeds allowable threshold ({settings.MaxAllowedAccuracyMeters}m). " +
                    "Authentic hardware GPS signal is required. Please ensure High Accuracy is enabled and move near an open window or entrance.");
            }

            // 3. Authenticity Validation: Timestamp Freshness (prevent replay attacks)
            var timeDifference = (DateTimeOffset.UtcNow - request.ClientTimestamp).Duration();
            if (timeDifference > TimeSpan.FromMinutes(3))
            {
                throw new InvalidOperationException("GPS coordinate timestamp is expired. Please acquire a fresh location reading.");
            }

            // 4. Physical Geofence Verification using Haversine formula
            var distance = CalculateDistanceInMeters(
                request.Latitude,
                request.Longitude,
                settings.CenterLatitude,
                settings.CenterLongitude);

            if (distance > settings.AllowedRadiusMeters)
            {
                logger.LogWarning("Geofence breach for student {Email}: {Distance}m away (limit: {Radius}m)",
                    student.Email, Math.Round(distance), settings.AllowedRadiusMeters);

                throw new InvalidOperationException(
                    $"Physical presence required. You are {Math.Round(distance)} meters outside the academy tutorial center " +
                    $"(Allowed radius: {settings.AllowedRadiusMeters}m). Please clock in inside the academy premises.");
            }

            // 5. Time Window & Lateness Calculation (Local Academy Time)
            var localNow = DateTimeOffset.UtcNow.ToOffset(LocalTimezoneOffset);
            var todayDate = DateOnly.FromDateTime(localNow.DateTime);
            var currentTime = TimeOnly.FromTimeSpan(localNow.TimeOfDay);

            if (currentTime < settings.EarliestClockInTime)
            {
                throw new InvalidOperationException(
                    $"Clock-in has not opened yet for today. Daily check-in begins at {settings.EarliestClockInTime:hh\\:mm tt}.");
            }

            if (currentTime > settings.LatestClockInTime)
            {
                throw new InvalidOperationException(
                    $"Daily self-check-in has closed for today (closed at {settings.LatestClockInTime:hh\\:mm tt}). " +
                    "Please consult your instructor or tutorial supervisor for manual verification.");
            }

            var status = currentTime > settings.LateCutoffTime
                ? AttendanceStatus.Late
                : AttendanceStatus.Present;

            // 6. Check for Existing Daily Record (Prevent Duplicates)
            var existingRecord = await dbContext.AttendanceRecords
                .Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.StudentId == studentId && r.Date == todayDate);

            if (existingRecord != null)
            {
                // Already clocked in today, return existing record
                return MapToDto(existingRecord);
            }

            var newRecord = new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                Date = todayDate,
                ClockInTime = DateTimeOffset.UtcNow,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                AccuracyMeters = Math.Round(request.AccuracyMeters, 1),
                DistanceMeters = Math.Round(distance, 1),
                Status = status,
                VerificationType = AttendanceVerificationType.Geolocated,
                Remarks = status == AttendanceStatus.Late
                    ? $"Arrived at {localNow:hh:mm tt} (Past {settings.LateCutoffTime:hh\\:mm tt} threshold)"
                    : $"On-time arrival ({Math.Round(distance, 1)}m from center)",
                CreatedAt = DateTimeOffset.UtcNow
            };

            dbContext.AttendanceRecords.Add(newRecord);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Student {Email} successfully clocked in ({Status}, {Distance}m)",
                student.Email, status, Math.Round(distance, 1));

            newRecord.Student = student;
            return MapToDto(newRecord);
        }

        public async Task<AttendanceRecordDto> ClockOutAsync(Guid studentId, ClockInRequestDto request)
        {
            var student = await userManager.FindByIdAsync(studentId.ToString())
                ?? throw new KeyNotFoundException("Student record not found.");

            if (!student.IsActive)
            {
                throw new InvalidOperationException("Your student account is currently suspended. Please contact the administration.");
            }

            var settings = await GetOrCreateSettingsInternalAsync();

            // 1. Authenticity Validation: Physical GPS Bounds
            if (request.Latitude < -90.0 || request.Latitude > 90.0 ||
                request.Longitude < -180.0 || request.Longitude > 180.0 ||
                (Math.Abs(request.Latitude) < 0.0001 && Math.Abs(request.Longitude) < 0.0001))
            {
                throw new ArgumentException("Invalid GPS coordinate values detected.");
            }

            // 2. Authenticity Validation: Hardware GPS Accuracy Threshold
            if (request.AccuracyMeters <= 0 || request.AccuracyMeters > settings.MaxAllowedAccuracyMeters)
            {
                throw new InvalidOperationException(
                    $"GPS accuracy ({Math.Round(request.AccuracyMeters)}m) exceeds allowable threshold ({settings.MaxAllowedAccuracyMeters}m). " +
                    "Authentic hardware GPS signal is required. Please ensure High Accuracy is enabled.");
            }

            // 3. Authenticity Validation: Timestamp Freshness
            var timeDifference = (DateTimeOffset.UtcNow - request.ClientTimestamp).Duration();
            if (timeDifference > TimeSpan.FromMinutes(3))
            {
                throw new InvalidOperationException("GPS coordinate timestamp is expired. Please acquire a fresh location reading.");
            }

            // 4. Physical Geofence Verification
            var distance = CalculateDistanceInMeters(
                request.Latitude,
                request.Longitude,
                settings.CenterLatitude,
                settings.CenterLongitude);

            if (distance > settings.AllowedRadiusMeters)
            {
                logger.LogWarning("Clock-out geofence breach for student {Email}: {Distance}m away (limit: {Radius}m)",
                    student.Email, Math.Round(distance), settings.AllowedRadiusMeters);

                throw new InvalidOperationException(
                    $"Physical presence required. You are {Math.Round(distance)} meters outside the academy center. " +
                    "Please clock out while inside the academy premises.");
            }

            // 5. Get Today's Record
            var localNow = DateTimeOffset.UtcNow.ToOffset(LocalTimezoneOffset);
            var todayDate = DateOnly.FromDateTime(localNow.DateTime);

            var existingRecord = await dbContext.AttendanceRecords
                .Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.StudentId == studentId && r.Date == todayDate);

            if (existingRecord == null)
            {
                throw new InvalidOperationException("No check-in record found for today. You must check in before checking out.");
            }

            if (existingRecord.ClockOutTime != null)
            {
                // Already clocked out
                return MapToDto(existingRecord);
            }

            existingRecord.ClockOutTime = DateTimeOffset.UtcNow;
            existingRecord.ClockOutLatitude = request.Latitude;
            existingRecord.ClockOutLongitude = request.Longitude;
            existingRecord.ClockOutAccuracyMeters = Math.Round(request.AccuracyMeters, 1);
            existingRecord.ClockOutDistanceMeters = Math.Round(distance, 1);
            existingRecord.UpdatedAt = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync();

            logger.LogInformation("Student {Email} successfully clocked out ({Distance}m)",
                student.Email, Math.Round(distance, 1));

            return MapToDto(existingRecord);
        }

        public async Task<StudentAttendanceStatsDto> GetStudentTodayStatusAsync(Guid studentId)
        {
            var localNow = DateTimeOffset.UtcNow.ToOffset(LocalTimezoneOffset);
            var todayDate = DateOnly.FromDateTime(localNow.DateTime);

            var todayRecord = await dbContext.AttendanceRecords
                .Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.StudentId == studentId && r.Date == todayDate);

            var pastRecords = await dbContext.AttendanceRecords
                .Where(r => r.StudentId == studentId)
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            var totalDays = pastRecords.Count;
            var presentDays = pastRecords.Count(r => r.Status == AttendanceStatus.Present);
            var lateDays = pastRecords.Count(r => r.Status == AttendanceStatus.Late);
            var absentDays = pastRecords.Count(r => r.Status == AttendanceStatus.Absent);

            var attendanceRate = totalDays > 0
                ? Math.Round(((double)(presentDays + lateDays) / totalDays) * 100.0, 1)
                : 100.0;

            // Calculate current streak of Present or Late days
            var streak = 0;
            foreach (var r in pastRecords)
            {
                if (r.Status == AttendanceStatus.Present || r.Status == AttendanceStatus.Late)
                {
                    streak++;
                }
                else
                {
                    break;
                }
            }

            return new StudentAttendanceStatsDto
            {
                Date = todayDate,
                HasClockedInToday = todayRecord != null && todayRecord.Status != AttendanceStatus.Absent,
                HasClockedOutToday = todayRecord?.ClockOutTime != null,
                TodayRecord = todayRecord != null ? MapToDto(todayRecord) : null,
                TotalDays = totalDays,
                PresentDays = presentDays,
                LateDays = lateDays,
                AbsentDays = absentDays,
                AttendanceRate = attendanceRate,
                PunctualStreak = streak
            };
        }

        public async Task<DailyRosterResponseDto> GetDailyRosterAsync(DateOnly date)
        {
            // 1. Get only users enrolled in the Student role (exclude instructors and admins)
            var studentUsers = await userManager.GetUsersInRoleAsync("Student");
            var students = studentUsers.OrderBy(u => u.FullName).ToList();

            // 2. Get attendance records for this date
            var records = await dbContext.AttendanceRecords
                .Where(r => r.Date == date)
                .ToDictionaryAsync(r => r.StudentId);

            var rosterItems = new List<RosterStudentItemDto>();

            foreach (var s in students)
            {
                records.TryGetValue(s.Id, out var record);

                rosterItems.Add(new RosterStudentItemDto
                {
                    StudentId = s.Id,
                    StudentName = s.FullName,
                    StudentEmail = s.Email ?? string.Empty,
                    PhoneNumber = s.PhoneNumber,
                    IsActive = s.IsActive,
                    Status = record?.Status ?? AttendanceStatus.Absent,
                    ClockInTime = record?.ClockInTime,
                    ClockOutTime = record?.ClockOutTime,
                    DistanceMeters = record?.DistanceMeters,
                    AccuracyMeters = record?.AccuracyMeters,
                    VerificationType = record?.VerificationType,
                    MarkedByUserName = record?.MarkedByUserName,
                    Remarks = record?.Remarks,
                    AttendanceRecordId = record?.Id
                });
            }

            return new DailyRosterResponseDto
            {
                Date = date,
                TotalEnrolled = rosterItems.Count,
                PresentCount = rosterItems.Count(r => r.Status == AttendanceStatus.Present),
                LateCount = rosterItems.Count(r => r.Status == AttendanceStatus.Late),
                AbsentCount = rosterItems.Count(r => r.Status == AttendanceStatus.Absent),
                ExcusedCount = rosterItems.Count(r => r.Status == AttendanceStatus.Excused),
                Students = rosterItems
            };
        }

        public async Task<AttendanceRecordDto> OverrideAttendanceAsync(
            Guid staffUserId,
            string staffUserName,
            AttendanceOverrideRequestDto request)
        {
            var student = await userManager.FindByIdAsync(request.StudentId.ToString())
                ?? throw new KeyNotFoundException("Student not found.");

            var record = await dbContext.AttendanceRecords
                .Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.StudentId == request.StudentId && r.Date == request.Date);

            if (record != null)
            {
                record.Status = request.Status;
                record.VerificationType = AttendanceVerificationType.ManualStaff;
                record.MarkedByUserId = staffUserId;
                record.MarkedByUserName = staffUserName;
                record.Remarks = string.IsNullOrWhiteSpace(request.Remarks)
                    ? $"Status updated to {request.Status} by {staffUserName}"
                    : request.Remarks.Trim();
                record.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                record = new AttendanceRecord
                {
                    Id = Guid.NewGuid(),
                    StudentId = request.StudentId,
                    Date = request.Date,
                    ClockInTime = DateTimeOffset.UtcNow,
                    Status = request.Status,
                    VerificationType = AttendanceVerificationType.ManualStaff,
                    MarkedByUserId = staffUserId,
                    MarkedByUserName = staffUserName,
                    Remarks = string.IsNullOrWhiteSpace(request.Remarks)
                        ? $"Marked as {request.Status} by {staffUserName}"
                        : request.Remarks.Trim(),
                    CreatedAt = DateTimeOffset.UtcNow
                };
                dbContext.AttendanceRecords.Add(record);
            }

            await dbContext.SaveChangesAsync();
            record.Student = student;
            return MapToDto(record);
        }

        public async Task<AttendanceSettingDto> GetSettingsAsync()
        {
            var entity = await GetOrCreateSettingsInternalAsync();
            return MapSettingsToDto(entity);
        }

        public async Task<AttendanceSettingDto> UpdateSettingsAsync(string updatedBy, AttendanceSettingDto request)
        {
            if (request.CenterLatitude < -90.0 || request.CenterLatitude > 90.0 ||
                request.CenterLongitude < -180.0 || request.CenterLongitude > 180.0)
            {
                throw new ArgumentException("Invalid coordinates provided.");
            }

            if (request.AllowedRadiusMeters < 20.0 || request.AllowedRadiusMeters > 5000.0)
            {
                throw new ArgumentException("Allowed radius must be between 20 meters and 5000 meters.");
            }

            var entity = await GetOrCreateSettingsInternalAsync();

            entity.CenterLatitude = request.CenterLatitude;
            entity.CenterLongitude = request.CenterLongitude;
            entity.AllowedRadiusMeters = request.AllowedRadiusMeters;
            entity.MaxAllowedAccuracyMeters = request.MaxAllowedAccuracyMeters > 0 ? request.MaxAllowedAccuracyMeters : 80.0;

            if (TimeOnly.TryParse(request.EarliestClockInTime, out var earliest))
            {
                entity.EarliestClockInTime = earliest;
            }
            if (TimeOnly.TryParse(request.LateCutoffTime, out var late))
            {
                entity.LateCutoffTime = late;
            }
            if (TimeOnly.TryParse(request.LatestClockInTime, out var latest))
            {
                entity.LatestClockInTime = latest;
            }

            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.UpdatedBy = updatedBy;

            await dbContext.SaveChangesAsync();
            return MapSettingsToDto(entity);
        }

        private async Task<AttendanceSetting> GetOrCreateSettingsInternalAsync()
        {
            var setting = await dbContext.AttendanceSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new AttendanceSetting
                {
                    Id = Guid.NewGuid(),
                    CenterLatitude = 6.5244,
                    CenterLongitude = 3.3792,
                    AllowedRadiusMeters = 100.0,
                    MaxAllowedAccuracyMeters = 80.0,
                    EarliestClockInTime = new TimeOnly(7, 0),
                    LateCutoffTime = new TimeOnly(8, 30),
                    LatestClockInTime = new TimeOnly(13, 0),
                    UpdatedAt = DateTimeOffset.UtcNow,
                    UpdatedBy = "System Default"
                };
                dbContext.AttendanceSettings.Add(setting);
                await dbContext.SaveChangesAsync();
            }
            return setting;
        }

        public static double CalculateDistanceInMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double EarthRadiusMeters = 6371000.0;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);
            var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
            return EarthRadiusMeters * c;
        }

        private static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);

        public async Task<byte[]> GenerateAttendanceCsvReportAsync(
            DateOnly? startDate,
            DateOnly? endDate,
            Guid? studentId,
            string? searchTerm)
        {
            var query = dbContext.AttendanceRecords
                .Include(r => r.Student)
                .AsNoTracking();

            if (startDate.HasValue)
            {
                query = query.Where(r => r.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.Date <= endDate.Value);
            }

            if (studentId.HasValue && studentId.Value != Guid.Empty)
            {
                query = query.Where(r => r.StudentId == studentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLowerInvariant();
                query = query.Where(r => r.Student.FullName.ToLower().Contains(term) ||
                                         (r.Student.Email != null && r.Student.Email.ToLower().Contains(term)));
            }

            var records = await query
                .OrderByDescending(r => r.Date)
                .ThenBy(r => r.ClockInTime)
                .ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("\"Record ID\",\"Student Name\",\"Email\",\"Phone Number\",\"Date\",\"Clock-In (WAT)\",\"Clock-Out (WAT)\",\"Duration (Hours)\",\"Status\",\"Verification Method\",\"Distance from Center (m)\",\"Remarks\"");

            foreach (var r in records)
            {
                var localIn = r.ClockInTime.ToOffset(LocalTimezoneOffset).ToString("hh:mm:ss tt");
                var localOut = r.ClockOutTime.HasValue
                    ? r.ClockOutTime.Value.ToOffset(LocalTimezoneOffset).ToString("hh:mm:ss tt")
                    : "N/A";

                var durationStr = "N/A";
                if (r.ClockOutTime.HasValue)
                {
                    var duration = r.ClockOutTime.Value - r.ClockInTime;
                    durationStr = $"{Math.Max(0, duration.TotalHours):F2}";
                }

                var studentName = EscapeCsv(r.Student?.FullName ?? "Student");
                var email = EscapeCsv(r.Student?.Email ?? string.Empty);
                var phone = EscapeCsv(r.Student?.PhoneNumber ?? string.Empty);
                var dateStr = r.Date.ToString("yyyy-MM-dd");
                var status = r.Status.ToString();
                var verification = r.VerificationType.ToString();
                var distance = r.DistanceMeters.HasValue ? $"{r.DistanceMeters.Value:F1}" : "N/A";
                var remarks = EscapeCsv(r.Remarks ?? string.Empty);

                sb.AppendLine($"\"{r.Id}\",\"{studentName}\",\"{email}\",\"{phone}\",\"{dateStr}\",\"{localIn}\",\"{localOut}\",\"{durationStr}\",\"{status}\",\"{verification}\",\"{distance}\",\"{remarks}\"");
            }

            return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\"", "\"\"");
        }

        private static AttendanceRecordDto MapToDto(AttendanceRecord entity) => new()
        {
            Id = entity.Id,
            StudentId = entity.StudentId,
            StudentName = entity.Student?.FullName ?? "Student",
            StudentEmail = entity.Student?.Email ?? string.Empty,
            StudentPhone = entity.Student?.PhoneNumber,
            Date = entity.Date,
            ClockInTime = entity.ClockInTime,
            ClockOutTime = entity.ClockOutTime,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            AccuracyMeters = entity.AccuracyMeters,
            DistanceMeters = entity.DistanceMeters,
            ClockOutLatitude = entity.ClockOutLatitude,
            ClockOutLongitude = entity.ClockOutLongitude,
            ClockOutAccuracyMeters = entity.ClockOutAccuracyMeters,
            ClockOutDistanceMeters = entity.ClockOutDistanceMeters,
            Status = entity.Status,
            VerificationType = entity.VerificationType,
            MarkedByUserName = entity.MarkedByUserName,
            Remarks = entity.Remarks
        };

        private static AttendanceSettingDto MapSettingsToDto(AttendanceSetting entity) => new()
        {
            CenterLatitude = entity.CenterLatitude,
            CenterLongitude = entity.CenterLongitude,
            AllowedRadiusMeters = entity.AllowedRadiusMeters,
            MaxAllowedAccuracyMeters = entity.MaxAllowedAccuracyMeters,
            EarliestClockInTime = entity.EarliestClockInTime.ToString("HH:mm"),
            LateCutoffTime = entity.LateCutoffTime.ToString("HH:mm"),
            LatestClockInTime = entity.LatestClockInTime.ToString("HH:mm"),
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
    }
}
