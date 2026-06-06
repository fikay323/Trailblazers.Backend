using Microsoft.EntityFrameworkCore;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Infrastructure.Persistence
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Submission> Submissions => Set<Submission>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Submission>(entity =>
            {
                entity.ToTable("submissions");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id");

                entity.Property(e => e.Type)
                    .HasColumnName("type");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnName("name");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasColumnName("email");

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at");

                entity.Property(e => e.Metadata)
                    .IsRequired()
                    .HasColumnType("jsonb")
                    .HasColumnName("metadata");

                // Composite index on (Type, CreatedAt DESC) for efficient queries
                entity.HasIndex(e => new { e.Type, e.CreatedAt })
                    .HasDatabaseName("IX_submissions_type_created_at_desc")
                    .IsDescending(false, true);
            });
        }
    }
}
