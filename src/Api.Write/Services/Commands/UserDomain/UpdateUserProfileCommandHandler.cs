using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Abstraction;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands;
using ProjectZenith.Contracts.Events;
using ProjectZenith.Contracts.Models;
using System.Security.Claims;

namespace ProjectZenith.Api.Write.Services.Commands.UserDomain
{
    public class UpdateUserProfileCommandHandler
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<UpdateUserProfileCommand> _validator;

        public UpdateUserProfileCommandHandler(
            WriteDbContext dbContext,
            IEventPublisher eventPublisher,
            IValidator<UpdateUserProfileCommand> validator)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _validator = validator;
        }

        public async Task<UserProfileUpdatedEvent> HandleAsync(UpdateUserProfileCommand command, ClaimsPrincipal userPrincipal, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var authenticatedUserId = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (authenticatedUserId == null || !Guid.TryParse(authenticatedUserId, out var authUserId) || authUserId != command.UserId)
            {
                throw new UnauthorizedAccessException("User is not authorized to update this profile.");
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken)
                ?? throw new InvalidOperationException("User not found.");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                user.Username = command.DisplayName;
                user.Bio = command.Bio;

                _dbContext.SystemLogs.Add(new SystemLog
                {
                    UserId = user.Id,
                    Action = "ProfileUpdate",
                    Details = $"User {user.Email} updated profile: DisplayName={command.DisplayName}, Bio={(command.Bio != null ? "Updated" : "Not changed")}",
                    Timestamp = DateTime.UtcNow
                });
                try
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new InvalidOperationException("Profile update failed due to concurrent modification.");
                }

                var userEvent = new UserProfileUpdatedEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    UserName = user.Username,
                    Bio = user.Bio,
                    UpdatedAt = DateTime.UtcNow
                };
                await _eventPublisher.PublishAsync("user-events", userEvent, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return userEvent;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
