using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Infrastructure.Persistence;

namespace Trailblazers.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        ApplicationDbContext dbContext,
        ILogger<AuthController> logger) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Email and password are required." });
            }

            var existingUser = await userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
            if (existingUser != null)
            {
                return BadRequest(new { error = "An account with this email address already exists." });
            }

            var user = new ApplicationUser
            {
                UserName = request.Email.Trim().ToLowerInvariant(),
                Email = request.Email.Trim().ToLowerInvariant(),
                FullName = string.IsNullOrWhiteSpace(request.FullName) ? "Student" : request.FullName.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return BadRequest(new { error = errors });
            }

            // Assign Student role
            await userManager.AddToRoleAsync(user, "Student");

            var roles = await userManager.GetRolesAsync(user);
            var token = jwtTokenService.GenerateAccessToken(user, roles);
            var refreshToken = jwtTokenService.GenerateRefreshToken(user.Id);

            dbContext.RefreshTokens.Add(refreshToken);
            await dbContext.SaveChangesAsync();

            AppendAuthCookies(token, refreshToken.Token);

            logger.LogInformation("New student registered: {Email}", user.Email);

            return Created(string.Empty, new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken.Token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    Role = roles.FirstOrDefault() ?? "Student",
                    IsActive = user.IsActive,
                    DisabledReason = user.DisabledReason
                }
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Email and password are required." });
            }

            var user = await userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
            if (user == null)
            {
                return Unauthorized(new { error = "Invalid email or password." });
            }

            var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                return Unauthorized(new { error = "Invalid email or password." });
            }

            var roles = await userManager.GetRolesAsync(user);
            var token = jwtTokenService.GenerateAccessToken(user, roles);
            var refreshToken = jwtTokenService.GenerateRefreshToken(user.Id);

            dbContext.RefreshTokens.Add(refreshToken);
            await dbContext.SaveChangesAsync();

            AppendAuthCookies(token, refreshToken.Token);

            logger.LogInformation("User logged in: {Email} (Active: {IsActive})", user.Email, user.IsActive);

            return Ok(new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken.Token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    Role = roles.FirstOrDefault() ?? "Student",
                    IsActive = user.IsActive,
                    DisabledReason = user.DisabledReason
                }
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(new { error = "Refresh token is required." });
            }

            var storedToken = await dbContext.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

            if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return Unauthorized(new { error = "Invalid or expired refresh token." });
            }

            var user = storedToken.User;
            var roles = await userManager.GetRolesAsync(user);

            // Revoke old refresh token and issue a new pair
            storedToken.IsRevoked = true;
            var newRefreshToken = jwtTokenService.GenerateRefreshToken(user.Id);
            dbContext.RefreshTokens.Add(newRefreshToken);
            await dbContext.SaveChangesAsync();

            var token = jwtTokenService.GenerateAccessToken(user, roles);

            AppendAuthCookies(token, newRefreshToken.Token);

            return Ok(new AuthResponseDto
            {
                Token = token,
                RefreshToken = newRefreshToken.Token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    Role = roles.FirstOrDefault() ?? "Student",
                    IsActive = user.IsActive,
                    DisabledReason = user.DisabledReason
                }
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            ClearAuthCookies();
            return Ok(new { message = "Logged out successfully." });
        }

        private void AppendAuthCookies(string token, string refreshToken)
        {
            var cookieDomain = Environment.GetEnvironmentVariable("COOKIE_DOMAIN");
            var isSecure = !string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);

            var authCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isSecure,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            };

            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isSecure,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            };

            if (!string.IsNullOrWhiteSpace(cookieDomain))
            {
                authCookieOptions.Domain = cookieDomain;
                refreshCookieOptions.Domain = cookieDomain;
            }

            Response.Cookies.Append("auth_token", token, authCookieOptions);
            Response.Cookies.Append("refresh_token", refreshToken, refreshCookieOptions);
        }

        private void ClearAuthCookies()
        {
            var cookieDomain = Environment.GetEnvironmentVariable("COOKIE_DOMAIN");
            var isSecure = !string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isSecure,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(-1)
            };

            if (!string.IsNullOrWhiteSpace(cookieDomain))
            {
                cookieOptions.Domain = cookieDomain;
            }

            Response.Cookies.Append("auth_token", string.Empty, cookieOptions);
            Response.Cookies.Append("refresh_token", string.Empty, cookieOptions);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser([FromHeader(Name = "Authorization")] string? authHeader)
        {
            var emailClaim = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            if (string.IsNullOrWhiteSpace(emailClaim))
            {
                return Unauthorized(new { error = "Not authenticated." });
            }

            var user = await userManager.FindByEmailAsync(emailClaim);
            if (user == null)
            {
                return NotFound(new { error = "User not found." });
            }

            var roles = await userManager.GetRolesAsync(user);

            return Ok(new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? "Student",
                IsActive = user.IsActive,
                DisabledReason = user.DisabledReason
            });
        }
    }

    public class RegisterRequestDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public UserDto User { get; set; } = new();
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? DisabledReason { get; set; }
    }
}
