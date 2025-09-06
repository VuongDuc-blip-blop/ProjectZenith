using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Api.Write.Services.UserDomain.DomainServices.Security;
using ProjectZenith.Contracts.Commands.User;
using ProjectZenith.Contracts.Events.User;
using ProjectZenith.Contracts.Models;
using System.Security.Cryptography;
using ProjectZenith.Contracts.Infrastructure.Messaging;

namespace ProjectZenith.Api.Write.Services.UserDomain.CommandHandlers
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<ResetPasswordCommand> _validator;
        private readonly ITimeLimitedDataProtector _dataProtector;
        private readonly IPasswordService _passwordService;

        public ResetPasswordCommandHandler(WriteDbContext dbContext, IEventPublisher eventPublisher, IValidator<ResetPasswordCommand> validator, IDataProtectionProvider dataProtectorProvider, IPasswordService passwordService)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _dataProtector = dataProtectorProvider.CreateProtector("PasswordReset").ToTimeLimitedDataProtector();
            _passwordService = passwordService;
        }

        public async Task Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            string userIdString;
            try
            {
                userIdString = _dataProtector.Unprotect(command.ResetToken);
            }
            catch (CryptographicException)
            {
                throw new InvalidOperationException("Invalid or expired reset token");
            }

            if (!Guid.TryParse(userIdString, out var userId))
            {
                throw new InvalidOperationException("Invalid token format.");
            }

            var user = await _dbContext.Users
                .Include(u => u.Credential)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                ?? throw new InvalidOperationException("User not found");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                user.Credential.PasswordHash = _passwordService.HashPassword(command.NewPassword);

                //Invalidate all the refresh tokens after reset the password
                var refreshTokens = await _dbContext.RefreshTokens
                    .Where(u => u.Id == userId)
                    .ToListAsync(cancellationToken);

                if (refreshTokens.Any())
                {
                    _dbContext.RefreshTokens.RemoveRange(refreshTokens);
                }

                _dbContext.SystemLogs.Add(new SystemLog
                {
                    UserId = user.Id,
                    Action = "PasswordResetSuccess",
                    Details = $"Password successfully reset for user {user.Email}. All active sessions invalidated.",
                    Timestamp = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync(cancellationToken);

                // 4. Publish the completion event.
                var userEvent = new PasswordResetCompletedEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    ResetAt = DateTime.UtcNow
                };
                await _eventPublisher.PublishAsync(KafkaTopics.UserEvents, userEvent, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

    }
}
