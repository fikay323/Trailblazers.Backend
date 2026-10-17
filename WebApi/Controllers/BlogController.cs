using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Trailblazers.Backend.Core.Application.Features.Blog.Dtos;
using Trailblazers.Backend.Core.Application.Features.Blog.Interfaces;
using Trailblazers.Backend.WebApi.Authentication;

namespace Trailblazers.Backend.WebApi.Controllers
{
    [ApiController]
    public class BlogController : ControllerBase
    {
        private readonly IBlogService _blogService;
        private readonly ILogger<BlogController> _logger;

        public BlogController(IBlogService blogService, ILogger<BlogController> logger)
        {
            _blogService = blogService;
            _logger = logger;
        }

        // ==========================================
        // PUBLIC ENDPOINTS
        // ==========================================

        /// <summary>
        /// Retrieves published blog posts with pagination, category filter, and search.
        /// </summary>
        [HttpGet("api/blog")]
        public async Task<IActionResult> GetPublishedPosts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? category = null,
            [FromQuery] string? search = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _blogService.GetPublishedPostsAsync(page, pageSize, category, search, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve published blog posts");
                return StatusCode(500, new { error = "Failed to retrieve blog posts." });
            }
        }

        /// <summary>
        /// Retrieves a single published blog post by its unique URL slug and increments view count.
        /// </summary>
        [HttpGet("api/blog/{slug}")]
        public async Task<IActionResult> GetPostBySlug(
            string slug,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var post = await _blogService.GetPostBySlugAsync(slug, incrementViews: true, cancellationToken);
                if (post == null || !post.IsPublished)
                {
                    return NotFound(new { error = "Blog post not found." });
                }

                return Ok(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve blog post with slug {Slug}", slug);
                return StatusCode(500, new { error = "Failed to retrieve blog post." });
            }
        }

        // ==========================================
        // ADMIN / STAFF ENDPOINTS
        // ==========================================

        /// <summary>
        /// Admin: Retrieves all blog posts (drafts and published) with pagination and filters.
        /// </summary>
        [HttpGet("api/admin/blog")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> GetAdminPosts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? isPublished = null,
            [FromQuery] string? category = null,
            [FromQuery] string? search = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _blogService.GetAdminPostsAsync(page, pageSize, isPublished, category, search, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve admin blog posts");
                return StatusCode(500, new { error = "Failed to retrieve blog posts." });
            }
        }

        /// <summary>
        /// Admin: Retrieves a specific blog post by its GUID id.
        /// </summary>
        [HttpGet("api/admin/blog/{id:guid}")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> GetPostById(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var post = await _blogService.GetPostByIdAsync(id, cancellationToken);
                if (post == null)
                {
                    return NotFound(new { error = "Blog post not found." });
                }

                return Ok(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve admin blog post {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve blog post." });
            }
        }

        /// <summary>
        /// Admin: Creates a new blog post.
        /// </summary>
        [HttpPost("api/admin/blog")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> CreatePost(
            [FromBody] CreateBlogPostRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { error = "Title and Content are required." });
            }

            string authorName = "Academy Administration";
            Guid? authorId = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                authorName = User.Identity.Name ?? authorName;
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(idClaim, out var parsedId)) authorId = parsedId;
            }

            try
            {
                var created = await _blogService.CreatePostAsync(request, authorName, authorId, cancellationToken);
                return CreatedAtAction(nameof(GetPostBySlug), new { slug = created.Slug }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create blog post");
                return StatusCode(500, new { error = "Failed to create blog post." });
            }
        }

        /// <summary>
        /// Admin: Updates an existing blog post (title, slug, content, category, tags, status).
        /// </summary>
        [HttpPut("api/admin/blog/{id:guid}")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> UpdatePost(
            Guid id,
            [FromBody] UpdateBlogPostRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var updated = await _blogService.UpdatePostAsync(id, request, cancellationToken);
                if (updated == null)
                {
                    return NotFound(new { error = "Blog post not found." });
                }

                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update blog post {Id}", id);
                return StatusCode(500, new { error = "Failed to update blog post." });
            }
        }

        /// <summary>
        /// Admin: Deletes a blog post by its GUID id.
        /// </summary>
        [HttpDelete("api/admin/blog/{id:guid}")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> DeletePost(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var deleted = await _blogService.DeletePostAsync(id, cancellationToken);
                if (!deleted)
                {
                    return NotFound(new { error = "Blog post not found." });
                }

                return Ok(new { message = "Blog post deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete blog post {Id}", id);
                return StatusCode(500, new { error = "Failed to delete blog post." });
            }
        }
    }
}
