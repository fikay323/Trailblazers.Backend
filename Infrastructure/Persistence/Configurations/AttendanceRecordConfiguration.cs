using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Infrastructure.Persistence.Configurations
{
    public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
    {
        public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
        {
            builder.ToTable("attendance_records");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasColumnName("id");

            builder.Property(e => e.StudentId)
                .IsRequired()
                .HasColumnName("student_id");

            builder.Property(e => e.Date)
                .IsRequired()
                .HasColumnName("date");

            builder.Property(e => e.ClockInTime)
                .IsRequired()
                .HasColumnName("clock_in_time");

            builder.Property(e => e.Latitude)
                .HasColumnName("latitude");

            builder.Property(e => e.Longitude)
                .HasColumnName("longitude");

            builder.Property(e => e.AccuracyMeters)
                .HasColumnName("accuracy_meters");

            builder.Property(e => e.DistanceMeters)
                .HasColumnName("distance_meters");

            builder.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>()
                .HasColumnName("status");

            builder.Property(e => e.VerificationType)
                .IsRequired()
                .HasConversion<int>()
                .HasColumnName("verification_type");

            builder.Property(e => e.MarkedByUserId)
                .HasColumnName("marked_by_user_id");

            builder.Property(e => e.MarkedByUserName)
                .HasMaxLength(200)
                .HasColumnName("marked_by_user_name");

            builder.Property(e => e.Remarks)
                .HasMaxLength(500)
                .HasColumnName("remarks");

            builder.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnName("created_at");

            builder.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at");

            builder.HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique composite index: 1 attendance record per student per day
            builder.HasIndex(e => new { e.StudentId, e.Date })
                .IsUnique()
                .HasDatabaseName("ix_attendance_records_student_date");

            builder.HasIndex(e => e.Date)
                .HasDatabaseName("ix_attendance_records_date");
        }
    }
}
