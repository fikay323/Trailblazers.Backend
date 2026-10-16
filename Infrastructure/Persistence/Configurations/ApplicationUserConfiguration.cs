using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Infrastructure.Persistence.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("full_name");

            builder.Property(u => u.IsActive)
                .IsRequired()
                .HasDefaultValue(true)
                .HasColumnName("is_active");

            builder.Property(u => u.DisabledReason)
                .HasMaxLength(1000)
                .HasColumnName("disabled_reason");

			builder.Property(u => u.ProfilePictureUrl)
				.HasMaxLength(1000)
				.HasColumnName("profile_picture_url");

			builder.Property(u => u.ProfilePicturePublicId)
				.HasMaxLength(256)
				.HasColumnName("profile_picture_public_id");

			builder.Property(u => u.CreatedAt)
				.IsRequired()
				.HasColumnName("created_at");
		}
	}
}
