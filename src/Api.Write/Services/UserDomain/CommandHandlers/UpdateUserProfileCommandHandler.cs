using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Commands.User;
using ProjectZenith.Contracts.Events.User;
using ProjectZenith.Contracts.Models;
using System.Security.Claims;
using ProjectZenith.Contracts.Infrastructure.Messaging;

namespace ProjectZenith.Api.Write.Services.UserDomain.CommandHandlers
{
    public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<UpdateUserProfileCommand> _validator;
        private readonly IHttpContextAccessor _contextAccessor;

        public UpdateUserProfileCommandHandler(
            WriteDbContext dbContext,
            IEventPublisher eventPublisher,
            IValidator<UpdateUserProfileCommand> validator,
            IHttpContextAccessor contextAccessor)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _contextAccessor = contextAccessor;
        }

        public async Task Handle(UpdateUserProfileCommand command, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var userPrincipal = _contextAccessor.HttpContext?.User;
            if (userPrincipal == null)
            {
                throw new UnauthorizedAccessException("Could not determine the user identify from the current context");
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
                await _eventPublisher.PublishAsync(KafkaTopics.UserEvents, userEvent, cancellationToken);

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
