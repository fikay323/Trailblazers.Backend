using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Infrastructure.Persistence.Configurations
{
    public class ExamResultConfiguration : IEntityTypeConfiguration<ExamResult>
    {
        public void Configure(EntityTypeBuilder<ExamResult> builder)
        {
            builder.ToTable("exam_results");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .HasColumnName("id");

            builder.Property(r => r.SessionId)
                .IsRequired()
                .HasColumnName("session_id");

            builder.Property(r => r.CandidateId)
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnName("candidate_id");

            builder.Property(r => r.TotalScore)
                .IsRequired()
                .HasColumnName("total_score");

            builder.Property(r => r.CompletedAt)
                .IsRequired()
                .HasColumnName("completed_at");

            // Configure One-to-One relationship with ExamSession
            builder.HasOne(r => r.Session)
                .WithOne()
                .HasForeignKey<ExamResult>(r => r.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
