using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Infrastructure.Persistence.Configurations
{
    public class StudentNotificationConfiguration : IEntityTypeConfiguration<StudentNotification>
    {
        public void Configure(EntityTypeBuilder<StudentNotification> builder)
        {
            builder.ToTable("student_notifications");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.StudentId)
                .IsRequired();

            builder.Property(x => x.StudentEmail)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(x => x.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Message)
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(x => x.Type)
                .HasMaxLength(50)
                .HasDefaultValue("General")
                .IsRequired();

            builder.Property(x => x.IsRead)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.CreatedBy)
                .HasMaxLength(200);

            builder.HasIndex(x => x.StudentId);
            builder.HasIndex(x => x.StudentEmail);
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => x.IsRead);
        }
    }
}
