using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refresh_tokens");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .HasColumnName("id");

            builder.Property(t => t.Token)
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnName("token");

            builder.Property(t => t.ExpiresAt)
                .IsRequired()
                .HasColumnName("expires_at");

            builder.Property(t => t.IsRevoked)
                .IsRequired()
                .HasColumnName("is_revoked");

            builder.Property(t => t.CreatedAt)
                .IsRequired()
                .HasColumnName("created_at");

            builder.Property(t => t.UserId)
                .IsRequired()
                .HasColumnName("user_id");

            builder.HasOne(t => t.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(t => t.Token).IsUnique();
        }
    }
}
