using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Contracts.DTOs.User;
using ProjectZenith.Contracts.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ProjectZenith.Api.Write.Services.UserDomain.DomainServices.Security
{
    public class TokenService : ITokenService
    {
        private readonly JwtOptions _jwtOptions;
        private readonly IPasswordService _passwordService;
        private readonly WriteDbContext _dbContext;

        public TokenService(IOptions<JwtOptions> jwtOptions, IPasswordService passwordService, WriteDbContext dbContext)
        {
            _jwtOptions = jwtOptions.Value;
            _passwordService = passwordService;
            _dbContext = dbContext;
        }

        public async Task<LoginResponseDTO> GenerateTokenAsync(User user, string? deviceInfo, CancellationToken cancellationToken)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email,user.Email),
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()
                )
            };

            claims.AddRange(user.Roles.Select(ur => new Claim(ClaimTypes.Role, ur.Role.Name)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(_jwtOptions.ExpiryMinutes),
                signingCredentials: creds);

            var accessTokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Generate and store new refresh token
            var rawRefreshToken = Guid.NewGuid().ToString("N"); // Use a clean GUID
            var refreshTokenHash = _passwordService.HashPassword(rawRefreshToken);

            var refreshTokenEntry = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RefreshTokenHash = refreshTokenHash,
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                DeviceInfo = deviceInfo,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.RefreshTokens.Add(refreshTokenEntry);
            await _dbContext.SaveChangesAsync(cancellationToken);


            return new LoginResponseDTO
            {
                AccessToken = accessTokenString,
                RefreshTokenId = refreshTokenEntry.Id,
                RefreshToken = rawRefreshToken
            };

        }
    }
}
