using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Infrastructure.Persistence.Configurations
{
    public class AttendanceSettingConfiguration : IEntityTypeConfiguration<AttendanceSetting>
    {
        public void Configure(EntityTypeBuilder<AttendanceSetting> builder)
        {
            builder.ToTable("attendance_settings");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasColumnName("id");

            builder.Property(e => e.CenterLatitude)
                .IsRequired()
                .HasColumnName("center_latitude");

            builder.Property(e => e.CenterLongitude)
                .IsRequired()
                .HasColumnName("center_longitude");

            builder.Property(e => e.AllowedRadiusMeters)
                .IsRequired()
                .HasColumnName("allowed_radius_meters");

            builder.Property(e => e.MaxAllowedAccuracyMeters)
                .IsRequired()
                .HasColumnName("max_allowed_accuracy_meters");

            builder.Property(e => e.EarliestClockInTime)
                .IsRequired()
                .HasColumnName("earliest_clock_in_time");

            builder.Property(e => e.LateCutoffTime)
                .IsRequired()
                .HasColumnName("late_cutoff_time");

            builder.Property(e => e.LatestClockInTime)
                .IsRequired()
                .HasColumnName("latest_clock_in_time");

            builder.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasColumnName("updated_at");

            builder.Property(e => e.UpdatedBy)
                .HasMaxLength(200)
                .HasColumnName("updated_by");
        }
    }
}
