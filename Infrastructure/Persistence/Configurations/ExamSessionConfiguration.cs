using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Infrastructure.Persistence.Configurations
{
    public class ExamSessionConfiguration : IEntityTypeConfiguration<ExamSession>
    {
        public void Configure(EntityTypeBuilder<ExamSession> builder)
        {
            builder.ToTable("exam_sessions");

            builder.HasKey(s => s.Id);

            // Enable PostgreSQL xmin optimistic concurrency token
            builder.Property<uint>("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsRowVersion();

            builder.Property(s => s.Id)
                .HasColumnName("id");

            builder.Property(s => s.StudentEmail)
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnName("student_email");

            builder.Property(s => s.TargetYear)
                .IsRequired()
                .HasColumnName("target_year");

            builder.Property(s => s.StartTime)
                .IsRequired()
                .HasColumnName("start_time");

            builder.Property(s => s.EndTime)
                .IsRequired()
                .HasColumnName("end_time");

            builder.Property(s => s.IsCompleted)
                .IsRequired()
                .HasColumnName("is_completed");

            builder.Property(s => s.Score)
                .HasColumnName("score");

            builder.Property(s => s.AssignedQuestionIds)
                .IsRequired()
                .HasColumnName("assigned_question_ids")
                .HasColumnType("uuid[]")
                .HasDefaultValueSql("ARRAY[]::uuid[]");

            // Configure owned collection Answers mapped to student_answers table
            builder.OwnsMany(s => s.Answers, a =>
            {
                a.ToTable("student_answers");
                a.WithOwner().HasForeignKey("exam_session_id");
                a.Property<int>("id");
                a.HasKey("id");

                a.Property(sa => sa.QuestionId)
                    .IsRequired()
                    .HasColumnName("question_id");

                a.Property(sa => sa.SelectedOption)
                    .IsRequired()
                    .HasColumnName("selected_option");
            });

            // Index on StudentEmail for fast lookups
            builder.HasIndex(s => s.StudentEmail)
                .HasDatabaseName("IX_exam_sessions_student_email");
        }
    }
}
