using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Infrastructure.Services
{
    public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
    {
        public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
        {
            var secret = Environment.GetEnvironmentVariable("JWT_SECRET")
                         ?? configuration["JWT_SECRET"]
                         ?? "trailblazers-super-secure-jwt-key-2026-min-32-bytes";

            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
                         ?? configuration["JWT_ISSUER"]
                         ?? "trailblazers-api";

            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                           ?? configuration["JWT_AUDIENCE"]
                           ?? "trailblazers-client";

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Name, user.FullName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("is_active", user.IsActive.ToString().ToLowerInvariant())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public RefreshToken GenerateRefreshToken(Guid userId)
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = Convert.ToBase64String(randomBytes),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UserId = userId
            };
        }
    }
}
