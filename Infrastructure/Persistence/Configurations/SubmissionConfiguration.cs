using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Infrastructure.Persistence.Configurations
{
    public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
    {
        public void Configure(EntityTypeBuilder<Submission> builder)
        {
            builder.ToTable("submissions");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasColumnName("id");

            builder.Property(e => e.Type)
                .HasColumnName("type");

            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("name");

            builder.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnName("email");

            builder.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnName("created_at");

            builder.Property(e => e.Metadata)
                .IsRequired()
                .HasColumnType("jsonb")
                .HasColumnName("metadata");

            // Composite index on (Type, CreatedAt DESC) for efficient queries
            builder.HasIndex(e => new { e.Type, e.CreatedAt })
                .HasDatabaseName("IX_submissions_type_created_at_desc")
                .IsDescending(false, true);
        }
    }
}
