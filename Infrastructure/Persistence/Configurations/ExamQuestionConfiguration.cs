using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Infrastructure.Persistence.Configurations
{
    public class ExamQuestionConfiguration : IEntityTypeConfiguration<ExamQuestion>
    {
        public void Configure(EntityTypeBuilder<ExamQuestion> builder)
        {
            builder.ToTable("exam_questions");

            builder.HasKey(q => q.Id);

            builder.Property(q => q.Id)
                .HasColumnName("id");

            builder.Property(q => q.Subject)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("subject");

            builder.Property(q => q.ExamYear)
                .IsRequired()
                .HasColumnName("exam_year");

            builder.Property(q => q.QuestionText)
                .IsRequired()
                .HasColumnName("question_text");

            builder.Property(q => q.CorrectOption)
                .IsRequired()
                .HasColumnName("correct_option");

            builder.Property(q => q.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("image_url");

            builder.Property(q => q.ComprehensionPassage)
                .HasColumnName("comprehension_passage");

            // Map Options Dictionary<char, string> to PostgreSQL JSONB
            builder.Property(q => q.Options)
                .HasColumnType("jsonb")
                .HasColumnName("options")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<char, string>>(v, (JsonSerializerOptions?)null) ??
                         new Dictionary<char, string>()
                );

            // Composite index on (ExamYear, Subject)
            builder.HasIndex(q => new { q.ExamYear, q.Subject })
                .HasDatabaseName("IX_exam_questions_year_subject");
        }
    }
}
