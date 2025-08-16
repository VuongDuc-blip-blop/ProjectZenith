using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProjectZenith.Api.Write.Abstraction;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Api.Write.Services.Security;
using ProjectZenith.Contracts.Commands.User;
using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Contracts.DTOs.User;
using ProjectZenith.Contracts.Events.User;
using ProjectZenith.Contracts.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace ProjectZenith.Api.Write.Services.Commands.UserDomain
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDTO>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IPasswordService _passwordService;
        private readonly IEventPublisher _eventPublisher;
        private readonly JwtOptions _jwtOptions;
        private readonly IValidator<LoginCommand> _validator;

        public LoginCommandHandler(WriteDbContext dbContext, IPasswordService passwordService, IEventPublisher eventPublisher, IOptions<JwtOptions> jwtOptions, IValidator<LoginCommand> validator)
        {
            _dbContext = dbContext;
            _passwordService = passwordService;
            _eventPublisher = eventPublisher;
            _jwtOptions = jwtOptions.Value;
            _validator = validator;
        }

        public async Task<LoginResponseDTO> Handle(LoginCommand command, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var user = await _dbContext.Users
                .Include(u => u.Credential)
                .Include(u => u.Roles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == command.Email, cancellationToken)
                ?? throw new InvalidOperationException("Invalid email or password");

            if (!user.IsEmailVerified)
            {
                _dbContext.SystemLogs.Add(new SystemLog
                {
                    UserId = user.Id,
                    Action = "LoginAttempt",
                    Details = $"Email not verified for user {user.Email}",
                    Timestamp = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync(cancellationToken);
                throw new InvalidOperationException("Email is not verified.");
            }
            if (!_passwordService.VerifyPassword(command.Password, user.Credential.PasswordHash))
            {
                _dbContext.SystemLogs.Add(new SystemLog
                {
                    UserId = user.Id,
                    Action = "LoginAttempt",
                    Details = $"Invalid password for user {user.Email}",
                    Timestamp = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync(cancellationToken);
                throw new InvalidOperationException("Invalid email or password.");
            }

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email,user.Email),
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
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

            //Generate and store the refresh token
            var refreshToken = Guid.NewGuid().ToString();
            var refreshTokenId = Guid.NewGuid();
            var refreshTokenHash = _passwordService.HashPassword(refreshToken);
            var refreshTokenEntry = new RefreshToken
            {
                Id = refreshTokenId,
                UserId = user.Id,
                RefreshTokenHash = refreshTokenHash,
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                DeviceInfo = command.DeviceInfo, // Optional, from client
                CreatedAt = DateTime.UtcNow
            };
            var userEvent = new UserLoggedInEvent
            {
                UserId = user.Id,
                Email = user.Email,
                LoggedInAt = DateTime.UtcNow
            };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                _dbContext.RefreshTokens.Add(refreshTokenEntry);
                _dbContext.SystemLogs.Add(new SystemLog
                {
                    UserId = user.Id,
                    Action = "LoginSuccess",
                    Details = $"User {user.Email} logged in from device {command.DeviceInfo ?? "unknown"}",
                    Timestamp = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync(cancellationToken);


                await _eventPublisher.PublishAsync("user-events", userEvent, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            LoginResponseDTO loginResponseDTO = new LoginResponseDTO
            {
                AccessToken = tokenString,
                RefreshTokenId = refreshTokenId,
                RefreshToken = refreshToken,
            };
            return loginResponseDTO;

        }
    }
}
