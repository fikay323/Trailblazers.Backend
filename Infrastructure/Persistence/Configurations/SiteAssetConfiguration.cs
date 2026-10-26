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

			builder.HasKey(x => x.Id);

			builder.Property(x => x.Id)
				.HasColumnName("id");

			builder.Property(x => x.Key)
				.HasColumnName("key")
				.HasMaxLength(150)
				.IsRequired();

			builder.HasIndex(x => x.Key)
				.IsUnique()
				.HasDatabaseName("ix_site_assets_key");

			builder.Property(x => x.Group)
				.HasColumnName("group")
				.HasMaxLength(50)
				.IsRequired();

			builder.Property(x => x.ImageUrl)
				.HasColumnName("image_url")
				.HasMaxLength(1500)
				.IsRequired();

			builder.Property(x => x.PublicId)
				.HasColumnName("public_id")
				.HasMaxLength(300);

			builder.Property(x => x.AltText)
				.HasColumnName("alt_text")
				.HasMaxLength(500);

			builder.Property(x => x.Title)
				.HasColumnName("title")
				.HasMaxLength(300);

			builder.Property(x => x.Caption)
				.HasColumnName("caption")
				.HasMaxLength(1000);

			builder.Property(x => x.MetadataJson)
				.HasColumnName("metadata_json")
				.HasColumnType("text");

			builder.Property(x => x.SortOrder)
				.HasColumnName("sort_order")
				.HasDefaultValue(0)
				.IsRequired();

			builder.Property(x => x.UpdatedAt)
				.HasColumnName("updated_at")
				.IsRequired();

			builder.HasIndex(x => x.Group)
				.HasDatabaseName("ix_site_assets_group");

			builder.HasIndex(x => new { x.Group, x.SortOrder })
				.HasDatabaseName("ix_site_assets_group_sort_order");
		}
	}
}
