using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Commands.User;
using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Contracts.DTOs.User;
using ProjectZenith.Contracts.Events.User;
using ProjectZenith.Contracts.Models;
using ProjectZenith.Contracts.Infrastructure.Messaging;
using ProjectZenith.Api.Write.Services.UserDomain.DomainServices.Security;

namespace ProjectZenith.Api.Write.Services.UserDomain.CommandHandlers
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponseDTO>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IPasswordService _passwordService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<RefreshTokenCommand> _validator;
        private readonly JwtOptions _jwtOptions;
        private readonly ITokenService _tokenService;

        public RefreshTokenCommandHandler(
            WriteDbContext dbContext,
            IPasswordService passwordService,
            IEventPublisher eventPublisher,
            IValidator<RefreshTokenCommand> validator,
            IOptions<JwtOptions> jwtOptions,
            ITokenService tokenService)
        {
            _dbContext = dbContext;
            _passwordService = passwordService;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _jwtOptions = jwtOptions.Value;
            _tokenService = tokenService;
        }

        public async Task<RefreshTokenResponseDTO> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
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

            var genaratedRefreshToken = await _tokenService.GenerateTokenAsync(user, deviceInfo: string.Empty, cancellationToken);

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
                _dbContext.SystemLogs.Add(new SystemLog
                {
                    UserId = user.Id,
                    Action = "RefreshTokenSuccess",
                    Details = $"Session refreshed for user {user.Email} on device {refreshToken.DeviceInfo ?? "unknown"}",
                    Timestamp = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync(cancellationToken);

                // Publish event
                await _eventPublisher.PublishAsync(KafkaTopics.UserEvents, userEvent, cancellationToken);

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

            RefreshTokenResponseDTO refreshTokenResult = new RefreshTokenResponseDTO
            {
                AccessToken = genaratedRefreshToken.AccessToken,
                NewRefreshTokenId = genaratedRefreshToken.RefreshTokenId,
                NewRefreshToken = genaratedRefreshToken.RefreshToken,
            };

            return refreshTokenResult;


        }
    }
}
