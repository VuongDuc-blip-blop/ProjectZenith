using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProjectZenith.Api.Write.Abstraction;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Api.Write.Services.Security;
using ProjectZenith.Contracts.Commands;
using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Contracts.Events;
using ProjectZenith.Contracts.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ProjectZenith.Api.Write.Services.Commands.UserDomain
{
    public class RefreshTokenCommandHandler
    {
        private readonly WriteDbContext _dbContext;
        private readonly IPasswordService _passwordService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<RefreshTokenCommand> _validator;
        private readonly JwtOptions _jwtOptions;

        public RefreshTokenCommandHandler(WriteDbContext dbContext, IPasswordService passwordService, IEventPublisher eventPublisher, IValidator<RefreshTokenCommand> validator, IOptions<JwtOptions> jwtOptions)
        {
            _dbContext = dbContext;
            _passwordService = passwordService;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _jwtOptions = jwtOptions.Value;
        }

        public async Task<(string Token, Guid NewRefreshTokenId, string NewRefreshToken, UserSessionRefreshedEvent Event)> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var refreshToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Id == command.RefreshTokenId, cancellationToken);

            if (refreshToken == null)
            {
                _dbContext.SystemLogs.Add(new SystemLog
                {
                    UserId = Guid.Empty,
                    Action = "RefreshTokenAttempt",
                    Details = $"Invalid refresh token ID: {command.RefreshTokenId}",
                    Timestamp = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync(cancellationToken);
                throw new InvalidOperationException("Invalid refresh token ID.");
            }

            var user = refreshToken.User;

            if (!_passwordService.VerifyPassword(command.RefreshToken, refreshToken.RefreshTokenHash))
            {
                throw new InvalidOperationException("Invalid or expired token");
            }

            if (!user.IsEmailVerified)
            {
                throw new InvalidOperationException("Email is not verified");
            }

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r.Role.Name)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes),
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Rotate refresh token
            var newRefreshToken = Guid.NewGuid().ToString();
            var newRefreshTokenId = Guid.NewGuid();
            var newRefreshTokenHash = _passwordService.HashPassword(newRefreshToken);
            var newRefreshTokenEntry = new RefreshToken
            {
                Id = newRefreshTokenId,
                UserId = user.Id,
                RefreshTokenHash = newRefreshTokenHash,
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                DeviceInfo = refreshToken.DeviceInfo, // Retain device info
                CreatedAt = DateTime.UtcNow
            };

            var userEvent = new UserSessionRefreshedEvent
            {
                UserId = user.Id,
                Email = user.Email,
                RefreshedAt = DateTime.UtcNow
            };

            // Save changes with concurrency handling
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                _dbContext.RefreshTokens.Remove(refreshToken);
                _dbContext.RefreshTokens.Add(newRefreshTokenEntry);
                _dbContext.SystemLogs.Add(new SystemLog
                {
                    UserId = user.Id,
                    Action = "RefreshTokenSuccess",
                    Details = $"Session refreshed for user {user.Email} on device {refreshToken.DeviceInfo ?? "unknown"}",
                    Timestamp = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync(cancellationToken);

                // Publish event
                await _eventPublisher.PublishAsync("user-events", userEvent, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new InvalidOperationException("Refresh token was already used or invalidated.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }



            return (tokenString, newRefreshTokenId, newRefreshToken, userEvent);
        }
    }
}
