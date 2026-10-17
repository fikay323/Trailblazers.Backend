using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Infrastructure.Persistence.Configurations
{
    public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
    {
        public void Configure(EntityTypeBuilder<BlogPost> builder)
        {
            builder.ToTable("blog_posts");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .HasMaxLength(250)
                .IsRequired();

            builder.Property(x => x.Slug)
                .HasMaxLength(250)
                .IsRequired();

            builder.Property(x => x.Excerpt)
                .HasMaxLength(600)
                .IsRequired();

            builder.Property(x => x.Content)
                .IsRequired();

            builder.Property(x => x.CoverImageUrl)
                .HasMaxLength(1000);

            builder.Property(x => x.Category)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.AuthorName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.IsPublished)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(x => x.ViewCount)
                .HasDefaultValue(0)
                .IsRequired();

            builder.HasIndex(x => x.Slug)
                .IsUnique();

            builder.HasIndex(x => x.IsPublished);
            builder.HasIndex(x => x.Category);
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => x.PublishedAt);
        }
    }
}
