using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Infrastructure.Persistence.Configurations
{
	public class SiteAssetConfiguration : IEntityTypeConfiguration<SiteAsset>
	{
		public void Configure(EntityTypeBuilder<SiteAsset> builder)
		{
			builder.ToTable("site_assets");

			builder.HasKey(x => x.Key);

			builder.Property(x => x.Key)
				.HasMaxLength(150)
				.IsRequired();

			builder.Property(x => x.Group)
				.HasMaxLength(50)
				.IsRequired();

			builder.Property(x => x.ImageUrl)
				.HasMaxLength(1500)
				.IsRequired();

			builder.Property(x => x.PublicId)
				.HasMaxLength(300);

			builder.Property(x => x.AltText)
				.HasMaxLength(500);

			builder.Property(x => x.Title)
				.HasMaxLength(300);

			builder.Property(x => x.Caption)
				.HasMaxLength(1000);

			builder.Property(x => x.MetadataJson)
				.HasColumnType("text");

			builder.Property(x => x.SortOrder)
				.HasDefaultValue(0)
				.IsRequired();

			builder.Property(x => x.UpdatedAt)
				.IsRequired();

			builder.HasIndex(x => x.Group);
			builder.HasIndex(x => new { x.Group, x.SortOrder });
		}
	}
}
