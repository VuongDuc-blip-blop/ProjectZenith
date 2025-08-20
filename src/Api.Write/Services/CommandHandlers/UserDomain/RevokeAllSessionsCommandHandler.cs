using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Api.Write.Infrastructure.Messaging;
using ProjectZenith.Contracts.Commands.User;
using ProjectZenith.Contracts.Events.User;
using ProjectZenith.Contracts.Models;
using System.Security.Claims;

namespace ProjectZenith.Api.Write.Services.Commands.UserDomain
{
    public class RevokeAllSessionsCommandHandler : IRequestHandler<RevokeAllSessionsCommand>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<RevokeAllSessionsCommand> _validator;
        private readonly IHttpContextAccessor _contextAccessor;

        public RevokeAllSessionsCommandHandler(WriteDbContext dbContext, IEventPublisher eventPublisher, IValidator<RevokeAllSessionsCommand> validator, IHttpContextAccessor contextAccessor)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _contextAccessor = contextAccessor;
        }
        public async Task Handle(RevokeAllSessionsCommand command, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var userPrincipal = _contextAccessor.HttpContext?.User;
            if (userPrincipal == null)
            {
                throw new UnauthorizedAccessException("Could not determine user identity from the current context.");
            }

            var authenticatedUserId = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (authenticatedUserId == null || !Guid.TryParse(authenticatedUserId, out var authUserId) || authUserId != command.UserId)
            {
                throw new UnauthorizedAccessException("User is not authorized to perform this action.");
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken)
                ?? throw new InvalidOperationException("User not found");

            var refreshTokens = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == command.UserId)
                .ToListAsync();

            if (!refreshTokens.Any())
            {
                _dbContext.SystemLogs.Add(new SystemLog
                {
                    UserId = user.Id,
                    Action = "RevokeAllSessionsAttempt",
                    Details = $"No active sessions found for user {user.Email}",
                    Timestamp = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                _dbContext.RefreshTokens.RemoveRange(refreshTokens);
                _dbContext.SystemLogs.Add(new SystemLog
                {
                    UserId = user.Id,
                    Action = "RevokeAllSessionsSuccess",
                    Details = $"All sessions revoked for user {user.Email}",
                    Timestamp = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync(cancellationToken);

                var userEvent = new UserAllSessionsRevokedEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    RevokedAt = DateTime.UtcNow
                };
                await _eventPublisher.PublishAsync("user-events", userEvent, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }

            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

        }
    }
}
