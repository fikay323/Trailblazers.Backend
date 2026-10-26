using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trailblazers.Backend.Core.Application.Features.SiteAssets.Dtos;
using Trailblazers.Backend.Core.Application.Features.SiteAssets.Interfaces;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Infrastructure.Persistence;

namespace Trailblazers.Backend.Infrastructure.Services
{
	public class SiteAssetService : ISiteAssetService
	{
		private readonly ApplicationDbContext _context;
		private readonly IImageStorageService _imageStorageService;
		private readonly ILogger<SiteAssetService> _logger;

		public SiteAssetService(
			ApplicationDbContext context,
			IImageStorageService imageStorageService,
			ILogger<SiteAssetService> logger)
		{
			_context = context;
			_imageStorageService = imageStorageService;
			_logger = logger;
		}

		public async Task<IReadOnlyList<SiteAssetDto>> GetAllAssetsAsync(string? group = null, CancellationToken cancellationToken = default)
		{
			await EnsureSeededAsync(cancellationToken);

			IQueryable<SiteAsset> query = _context.SiteAssets.AsNoTracking();

			if (!string.IsNullOrWhiteSpace(group))
			{
				var cleanGroup = group.Trim().ToLowerInvariant();
				query = query.Where(a => a.Group.ToLower() == cleanGroup);
			}

			var assets = await query
				.OrderBy(a => a.Group)
				.ThenBy(a => a.SortOrder)
				.ThenBy(a => a.Key)
				.ToListAsync(cancellationToken);

			return assets.Select(MapToDto).ToList();
		}

		public async Task<SiteAssetDto?> GetAssetByKeyAsync(string key, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				return null;
			}

			await EnsureSeededAsync(cancellationToken);

			var cleanKey = key.Trim();
			var asset = await _context.SiteAssets
				.AsNoTracking()
				.FirstOrDefaultAsync(a => a.Key == cleanKey, cancellationToken);

			return asset == null ? null : MapToDto(asset);
		}

		public async Task<SiteAssetDto> UpsertAssetAsync(string key, UpdateSiteAssetRequestDto request, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentException("Asset key cannot be empty", nameof(key));
			}

			if (string.IsNullOrWhiteSpace(request.ImageUrl))
			{
				throw new ArgumentException("Asset ImageUrl cannot be empty", nameof(request.ImageUrl));
			}

			var cleanKey = key.Trim();
			var asset = await _context.SiteAssets.FirstOrDefaultAsync(a => a.Key == cleanKey, cancellationToken);

			if (asset == null)
			{
				// Infer group from key (e.g. "home.hero.1" -> "home")
				var inferredGroup = !string.IsNullOrWhiteSpace(request.Group)
					? request.Group.Trim()
					: cleanKey.Contains('.') ? cleanKey.Split('.')[0] : "general";

				asset = new SiteAsset
				{
					Key = cleanKey,
					Group = inferredGroup,
					ImageUrl = request.ImageUrl.Trim(),
					PublicId = string.IsNullOrWhiteSpace(request.PublicId) ? null : request.PublicId.Trim(),
					AltText = request.AltText?.Trim(),
					Title = request.Title?.Trim(),
					Caption = request.Caption?.Trim(),
					MetadataJson = request.MetadataJson?.Trim(),
					SortOrder = request.SortOrder ?? 0,
					UpdatedAt = DateTimeOffset.UtcNow
				};

				_context.SiteAssets.Add(asset);
			}
			else
			{
				// If a new custom publicId is provided and differs from existing publicId, clean up the old asset in Cloudinary
				if (!string.IsNullOrWhiteSpace(asset.PublicId) &&
					!string.IsNullOrWhiteSpace(request.PublicId) &&
					asset.PublicId != request.PublicId.Trim())
				{
					try
					{
						_logger.LogInformation("Cleaning up replaced Cloudinary asset {PublicId} for key {Key}", asset.PublicId, cleanKey);
						await _imageStorageService.DeleteImageAsync(asset.PublicId, cancellationToken);
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Failed to delete old Cloudinary asset {PublicId}", asset.PublicId);
					}
				}

				if (!string.IsNullOrWhiteSpace(request.Group))
				{
					asset.Group = request.Group.Trim();
				}

				asset.ImageUrl = request.ImageUrl.Trim();
				if (request.PublicId != null)
				{
					asset.PublicId = string.IsNullOrWhiteSpace(request.PublicId) ? null : request.PublicId.Trim();
				}

				if (request.AltText != null) asset.AltText = request.AltText.Trim();
				if (request.Title != null) asset.Title = request.Title.Trim();
				if (request.Caption != null) asset.Caption = request.Caption.Trim();
				if (request.MetadataJson != null) asset.MetadataJson = request.MetadataJson.Trim();
				if (request.SortOrder.HasValue) asset.SortOrder = request.SortOrder.Value;
				asset.UpdatedAt = DateTimeOffset.UtcNow;
			}

			await _context.SaveChangesAsync(cancellationToken);
			return MapToDto(asset);
		}

		public async Task<bool> DeleteAssetAsync(string key, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				return false;
			}

			var cleanKey = key.Trim();
			var asset = await _context.SiteAssets.FirstOrDefaultAsync(a => a.Key == cleanKey, cancellationToken);

			if (asset == null)
			{
				return false;
			}

			// Clean up Cloudinary asset if present
			if (!string.IsNullOrWhiteSpace(asset.PublicId))
			{
				try
				{
					_logger.LogInformation("Deleting Cloudinary asset {PublicId} for deleted site asset {Key}", asset.PublicId, cleanKey);
					await _imageStorageService.DeleteImageAsync(asset.PublicId, cancellationToken);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Failed to delete Cloudinary asset {PublicId}", asset.PublicId);
				}
			}

			_context.SiteAssets.Remove(asset);
			await _context.SaveChangesAsync(cancellationToken);
			return true;
		}

		public async Task<bool> ReorderAssetsAsync(ReorderSiteAssetsRequestDto request, CancellationToken cancellationToken = default)
		{
			if (request.Items == null || request.Items.Count == 0)
			{
				return true;
			}

			var keys = request.Items.Select(i => i.Key.Trim()).ToList();
			var assets = await _context.SiteAssets
				.Where(a => keys.Contains(a.Key))
				.ToListAsync(cancellationToken);

			var lookup = request.Items.ToDictionary(i => i.Key.Trim(), i => i.SortOrder);
			foreach (var asset in assets)
			{
				if (lookup.TryGetValue(asset.Key, out var newSortOrder))
				{
					asset.SortOrder = newSortOrder;
					asset.UpdatedAt = DateTimeOffset.UtcNow;
				}
			}

			await _context.SaveChangesAsync(cancellationToken);
			return true;
		}

		private static SiteAssetDto MapToDto(SiteAsset asset)
		{
			return new SiteAssetDto
			{
				Key = asset.Key,
				Group = asset.Group,
				ImageUrl = asset.ImageUrl,
				PublicId = asset.PublicId,
				AltText = asset.AltText,
				Title = asset.Title,
				Caption = asset.Caption,
				MetadataJson = asset.MetadataJson,
				SortOrder = asset.SortOrder,
				UpdatedAt = asset.UpdatedAt
			};
		}

		private async Task EnsureSeededAsync(CancellationToken cancellationToken)
		{
			// Only seed if empty
			if (await _context.SiteAssets.AnyAsync(cancellationToken))
			{
				return;
			}

			_logger.LogInformation("Seeding default site asset slots into site_assets table...");

			var defaults = GetDefaultSiteAssets();
			_context.SiteAssets.AddRange(defaults);
			try
			{
				await _context.SaveChangesAsync(cancellationToken);
				_logger.LogInformation("Successfully seeded {Count} site asset slots.", defaults.Count);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to seed default site assets (may have been inserted concurrently)");
			}
		}

		private static List<SiteAsset> GetDefaultSiteAssets()
		{
			var now = DateTimeOffset.UtcNow;
			return new List<SiteAsset>
			{
				// BRANDING
				new() { Key = "brand.logo", Group = "branding", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg", PublicId = "trailblazers/branding/trailblazer", AltText = "Trailblazer Academy Crest", Title = "Main Site Logo", SortOrder = 1, UpdatedAt = now },

				// SEO OPEN GRAPH
				new() { Key = "seo.og.default", Group = "seo", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg", PublicId = "trailblazers/branding/trailblazer", AltText = "Trailblazer Academy & Edukonsult", Title = "Global Default OG Image", Caption = "Used across all pages unless a page-specific OG card is set.", SortOrder = 1, UpdatedAt = now },
				new() { Key = "seo.og.home", Group = "seo", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg", PublicId = "trailblazers/branding/trailblazer", AltText = "Trailblazer Academy Campus", Title = "Home Page OpenGraph Image", SortOrder = 2, UpdatedAt = now },
				new() { Key = "seo.og.about", Group = "seo", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg", PublicId = "trailblazers/branding/trailblazer", AltText = "Trailblazer Academy Campus", Title = "About Page OpenGraph Image", SortOrder = 3, UpdatedAt = now },
				new() { Key = "seo.og.summer_coaching", Group = "seo", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg", PublicId = "trailblazers/branding/trailblazer", AltText = "Trailblazer Academy Summer Coaching Program", Title = "Summer Coaching OpenGraph Image", Caption = "Featured preview card when summer coaching link is shared on WhatsApp/socials.", SortOrder = 4, UpdatedAt = now },
				new() { Key = "seo.og.programs", Group = "seo", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg", PublicId = "trailblazers/branding/trailblazer", AltText = "Students studying at Trailblazer Academy", Title = "Programs Page OpenGraph Image", SortOrder = 5, UpdatedAt = now },
				new() { Key = "seo.og.gallery", Group = "seo", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg", PublicId = "trailblazers/branding/trailblazer", AltText = "Trailblazer Academy campus facilities", Title = "Campus Gallery OpenGraph Image", SortOrder = 6, UpdatedAt = now },
				new() { Key = "seo.og.contact", Group = "seo", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg", PublicId = "trailblazers/branding/trailblazer", AltText = "Trailblazer Academy campus — contact us", Title = "Contact Page OpenGraph Image", SortOrder = 7, UpdatedAt = now },
				new() { Key = "seo.og.exam", Group = "seo", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg", PublicId = "trailblazers/branding/trailblazer", AltText = "UTME CBT Mock Exam at Trailblazer Academy", Title = "CBT Exam Page OpenGraph Image", SortOrder = 8, UpdatedAt = now },
				new() { Key = "seo.og.register", Group = "seo", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051828/trailblazers/branding/trailblazer.jpg", PublicId = "trailblazers/branding/trailblazer", AltText = "Register at Trailblazer Academy", Title = "Register Page OpenGraph Image", SortOrder = 9, UpdatedAt = now },

				// HOME HERO CAROUSEL
				new() { Key = "home.hero.1", Group = "home", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051836/trailblazers/gallery/image-7.jpg", PublicId = "trailblazers/gallery/image-7", AltText = "JAMB/CBT Prep", Title = "JAMB/CBT Prep", Caption = "Complete JAMB CBT training with professional guidance.", SortOrder = 1, UpdatedAt = now },
				new() { Key = "home.hero.2", Group = "home", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051829/trailblazers/gallery/image-1.jpg", PublicId = "trailblazers/gallery/image-1", AltText = "WAEC Classroom", Title = "WAEC Classroom", Caption = "Comprehensive WAEC subject lectures and exam prep.", SortOrder = 2, UpdatedAt = now },
				new() { Key = "home.hero.3", Group = "home", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051830/trailblazers/gallery/image-2.jpg", PublicId = "trailblazers/gallery/image-2", AltText = "NECO & GCE Tutoring", Title = "NECO & GCE Tutoring", Caption = "Intensive GCE & NECO tutoring for excellent results.", SortOrder = 3, UpdatedAt = now },
				new() { Key = "home.hero.4", Group = "home", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051831/trailblazers/gallery/image-3.jpg", PublicId = "trailblazers/gallery/image-3", AltText = "Our Classrooms", Title = "Our Classrooms", Caption = "Active, collaborative learning in a productive environment.", SortOrder = 4, UpdatedAt = now },
				new() { Key = "home.hero.5", Group = "home", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051833/trailblazers/gallery/image-5.jpg", PublicId = "trailblazers/gallery/image-5", AltText = "Focused Study", Title = "Focused Study", Caption = "Quiet spaces dedicated to focused self-study.", SortOrder = 5, UpdatedAt = now },
				new() { Key = "home.hero.6", Group = "home", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051834/trailblazers/gallery/image-6.jpg", PublicId = "trailblazers/gallery/image-6", AltText = "Expert Instructors", Title = "Expert Instructors", Caption = "Learn from experienced tutors dedicated to your academic success.", SortOrder = 6, UpdatedAt = now },
				new() { Key = "home.hero.7", Group = "home", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051836/trailblazers/gallery/image-7.jpg", PublicId = "trailblazers/gallery/image-7", AltText = "Subject Counseling", Title = "Subject Counseling", Caption = "Guidance on choosing the right JAMB and WAEC subject combinations.", SortOrder = 7, UpdatedAt = now },
				new() { Key = "home.hero.8", Group = "home", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051834/trailblazers/gallery/image-6.jpg", PublicId = "trailblazers/gallery/image-6", AltText = "Conducive Environment", Title = "Conducive Environment", Caption = "Comfortable, well-ventilated spaces designed to maximize your focus.", SortOrder = 8, UpdatedAt = now },

				// ABOUT PAGE SLOTS
				new() { Key = "about.hero.background", Group = "about", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051833/trailblazers/gallery/image-5.jpg", PublicId = "trailblazers/gallery/image-5", AltText = "Student illustration", Title = "About Hero Background Card", SortOrder = 1, UpdatedAt = now },
				new() { Key = "about.hero.foreground", Group = "about", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051831/trailblazers/gallery/image-3.jpg", PublicId = "trailblazers/gallery/image-3", AltText = "Teacher explaining", Title = "About Hero Foreground Card", SortOrder = 2, UpdatedAt = now },
				new() { Key = "about.objectives.banner", Group = "about", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051839/trailblazers/gallery/image-10.jpg", PublicId = "trailblazers/gallery/image-10", AltText = "Students in classroom", Title = "Organizational Objectives Banner", SortOrder = 3, UpdatedAt = now },
				new() { Key = "about.cbt_lab", Group = "about", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051839/trailblazers/gallery/image-10.jpg", PublicId = "trailblazers/gallery/image-10", AltText = "Dedicated CBT Lab", Title = "Dedicated CBT Lab Spotlight", SortOrder = 4, UpdatedAt = now },

				// PROGRAMS
				new() { Key = "programs.hero", Group = "programs", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051830/trailblazers/gallery/image-2.jpg", PublicId = "trailblazers/gallery/image-2", AltText = "Classroom setting", Title = "Programs Hero Visual", SortOrder = 1, UpdatedAt = now },

				// CAMPUS GALLERY (11 default images)
				new() { Key = "gallery.item.1", Group = "gallery", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051833/trailblazers/gallery/image-5.jpg", PublicId = "trailblazers/gallery/image-5", AltText = "Trailblazer Academy building exterior", Title = "Building Exterior", MetadataJson = "{\"className\":\"md:row-span-2 h-125\"}", SortOrder = 1, UpdatedAt = now },
				new() { Key = "gallery.item.2", Group = "gallery", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051829/trailblazers/gallery/image-1.jpg", PublicId = "trailblazers/gallery/image-1", AltText = "Students in a modern classroom setting", Title = "Modern Classroom", MetadataJson = "{\"className\":\"h-62.5\"}", SortOrder = 2, UpdatedAt = now },
				new() { Key = "gallery.item.3", Group = "gallery", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051830/trailblazers/gallery/image-2.jpg", PublicId = "trailblazers/gallery/image-2", AltText = "Students working on coding projects", Title = "Coding & CBT Projects", MetadataJson = "{\"className\":\"h-62.5\"}", SortOrder = 3, UpdatedAt = now },
				new() { Key = "gallery.item.4", Group = "gallery", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051831/trailblazers/gallery/image-3.jpg", PublicId = "trailblazers/gallery/image-3", AltText = "Student studying in the academy library", Title = "Academy Library", MetadataJson = "{\"className\":\"md:col-span-2 h-62.5\"}", SortOrder = 4, UpdatedAt = now },
				new() { Key = "gallery.item.5", Group = "gallery", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051832/trailblazers/gallery/image-4.jpg", PublicId = "trailblazers/gallery/image-4", AltText = "Science laboratory equipment and students", Title = "Science Laboratory", MetadataJson = "{\"className\":\"md:row-span-2 h-75\"}", SortOrder = 5, UpdatedAt = now },
				new() { Key = "gallery.item.6", Group = "gallery", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051834/trailblazers/gallery/image-6.jpg", PublicId = "trailblazers/gallery/image-6", AltText = "Outdoor campus area for students", Title = "Outdoor Campus", MetadataJson = "{\"className\":\"h-62.5\"}", SortOrder = 6, UpdatedAt = now },
				new() { Key = "gallery.item.7", Group = "gallery", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051836/trailblazers/gallery/image-7.jpg", PublicId = "trailblazers/gallery/image-7", AltText = "Collaborative workspace for group projects", Title = "Collaborative Workspace", MetadataJson = "{\"className\":\"h-62.5\"}", SortOrder = 7, UpdatedAt = now },
				new() { Key = "gallery.item.8", Group = "gallery", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051837/trailblazers/gallery/image-8.jpg", PublicId = "trailblazers/gallery/image-8", AltText = "Students engaged in a seminar", Title = "Seminar Hall", MetadataJson = "{\"className\":\"md:col-span-2 h-62.5\"}", SortOrder = 8, UpdatedAt = now },
				new() { Key = "gallery.item.9", Group = "gallery", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051838/trailblazers/gallery/image-9.jpg", PublicId = "trailblazers/gallery/image-9", AltText = "Modern technology lab", Title = "Technology Lab", MetadataJson = "{\"className\":\"md:col-span-2 h-62.5\"}", SortOrder = 9, UpdatedAt = now },
				new() { Key = "gallery.item.10", Group = "gallery", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051839/trailblazers/gallery/image-10.jpg", PublicId = "trailblazers/gallery/image-10", AltText = "Sports and recreational facilities", Title = "Sports Facilities", MetadataJson = "{\"className\":\"h-62.5\"}", SortOrder = 10, UpdatedAt = now },
				new() { Key = "gallery.item.11", Group = "gallery", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051841/trailblazers/gallery/image-11.jpg", PublicId = "trailblazers/gallery/image-11", AltText = "Student lounge and relaxation area", Title = "Student Lounge", MetadataJson = "{\"className\":\"md:col-span-3 h-62.5\"}", SortOrder = 11, UpdatedAt = now },

				// MANAGEMENT TEAM (8 members)
				new() { Key = "team.member.1", Group = "team", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051841/trailblazers/team/akintunde-gbadamosi.jpg", PublicId = "trailblazers/team/akintunde-gbadamosi", AltText = "Mr. Akintunde Gbadamosi", Title = "Mr. Akintunde Gbadamosi", Caption = "Proprietor", SortOrder = 1, UpdatedAt = now },
				new() { Key = "team.member.2", Group = "team", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051843/trailblazers/team/tolulope-gbadamosi.jpg", PublicId = "trailblazers/team/tolulope-gbadamosi", AltText = "Dr. Tolulope Victoria Gbadamosi", Title = "Dr. Tolulope Victoria Gbadamosi", Caption = "Proprietress / Founder & Director", SortOrder = 2, UpdatedAt = now },
				new() { Key = "team.member.3", Group = "team", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051844/trailblazers/team/adeshola-andrew.jpg", PublicId = "trailblazers/team/adeshola-andrew", AltText = "Mr. Adepoju Adeshola Andrew", Title = "Mr. Adepoju Adeshola Andrew", Caption = "Head of Academics", SortOrder = 3, UpdatedAt = now },
				new() { Key = "team.member.4", Group = "team", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051845/trailblazers/team/rotimi-akani.jpg", PublicId = "trailblazers/team/rotimi-akani", AltText = "Dr. Olurotimi Oladayo Akanni", Title = "Dr. Olurotimi Oladayo Akanni", Caption = "Management Member / Senior Lecturer", SortOrder = 4, UpdatedAt = now },
				new() { Key = "team.member.5", Group = "team", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051846/trailblazers/team/oluwatoyin-awolola.jpg", PublicId = "trailblazers/team/oluwatoyin-awolola", AltText = "Dr. Oluwatoyin Isaiah Awolola", Title = "Dr. Oluwatoyin Isaiah Awolola", Caption = "Management Member / Senior Lecturer", SortOrder = 5, UpdatedAt = now },
				new() { Key = "team.member.6", Group = "team", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051847/trailblazers/team/louis-odubanjo.jpg", PublicId = "trailblazers/team/louis-odubanjo", AltText = "Mr. Louis Odubanjo", Title = "Mr. Louis Odubanjo", Caption = "Management Member / Administrator", SortOrder = 6, UpdatedAt = now },
				new() { Key = "team.member.7", Group = "team", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051848/trailblazers/team/david-gbadamosi.jpg", PublicId = "trailblazers/team/david-gbadamosi", AltText = "Mr. David Erioluwa Gbadamosi", Title = "Mr. David Erioluwa Gbadamosi", Caption = "Data Analyst & Management Member", SortOrder = 7, UpdatedAt = now },
				new() { Key = "team.member.8", Group = "team", ImageUrl = "https://res.cloudinary.com/dssjoxdom/image/upload/v1793051849/trailblazers/team/boluwatife-ayano.jpg", PublicId = "trailblazers/team/boluwatife-ayano", AltText = "Boluwatife Ayano", Title = "Boluwatife Ayano", Caption = "Management Member", SortOrder = 8, UpdatedAt = now }
			};
		}
	}
}
