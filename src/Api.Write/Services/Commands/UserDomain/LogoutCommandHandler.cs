using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Abstraction;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Api.Write.Services.Security;
using ProjectZenith.Contracts.Commands;
using ProjectZenith.Contracts.Events;
using ProjectZenith.Contracts.Models;
using System.Security.Claims;

namespace ProjectZenith.Api.Write.Services.Commands.UserDomain
{
    public class LogoutCommandHandler
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<LogoutCommand> _validator;
        // NOTE: We no longer need IPasswordService if we store the raw token,
        // but we WILL need it if you stick with the hashed token model.
        // Let's assume the hashed token model for this answer.
        private readonly IPasswordService _passwordService;


        public LogoutCommandHandler(
            WriteDbContext dbContext,
            IEventPublisher eventPublisher,
            IValidator<LogoutCommand> validator,
            IPasswordService passwordService) // Add IPasswordService if using hashes
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _passwordService = passwordService;
        }

        // The handler now receives the command (from the request body) and the
        // user's identity (from the JWT).
        public async Task HandleAsync(LogoutCommand command, ClaimsPrincipal userPrincipal, CancellationToken cancellationToken)
        {
            // 1. Validate the incoming command DTO (e.g., check that RefreshToken is not empty)
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            // 2. Get the ID of the user MAKING the request from their secure JWT.
            // This is the ONLY trusted source for the user's identity.
            var userIdString = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var authenticatedUserId))
            {
                // This should theoretically never happen if the [Authorize] attribute is used.
                throw new UnauthorizedAccessException("Invalid user identity in token.");
            }

            // 3. Find the specific session record to be invalidated.


            // OPTION B: If you store the HASHED Refresh Token (Your current path)
            // This is less efficient but works.
            var userSessions = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == authenticatedUserId && rt.RefreshTokenExpiresAt > DateTimeOffset.UtcNow)
                .ToListAsync(cancellationToken);

            RefreshToken? sessionToInvalidate = null;
            foreach (var session in userSessions)
            {
                if (_passwordService.VerifyPassword(command.RefreshToken, session.RefreshTokenHash))
                {
                    sessionToInvalidate = session;
                    break;
                }
            }

            // Get the user's email for the log/event.
            var userEmail = userPrincipal.FindFirstValue(ClaimTypes.Email) ?? "N/A";

            var userEvent = new UserLoggedOutEvent
            {
                UserId = authenticatedUserId,
                Email = userEmail,
                LoggedOutAt = DateTime.UtcNow
            };

            // 4. If a session is found, verify ownership and invalidate it.
            if (sessionToInvalidate != null)
            {
                // 4a. SECURITY CHECK: Does this session belong to the person making the request?
                // This prevents the "Eve logs out Alice" attack.
                if (sessionToInvalidate.UserId != authenticatedUserId)
                {
                    // Log a security warning here!
                    throw new UnauthorizedAccessException("You are not authorized to terminate this session.");
                }


                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // 4b. Invalidate the session by deleting it.
                    _dbContext.RefreshTokens.Remove(sessionToInvalidate);

                    // 4c. Log the action
                    var log = new SystemLog
                    {
                        UserId = authenticatedUserId,
                        Action = "Logout",
                        Details = $"User {userEmail} logged out session {sessionToInvalidate.Id}.",
                        Timestamp = DateTime.UtcNow
                    };
                    _dbContext.SystemLogs.Add(log);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await _eventPublisher.PublishAsync("user-events", userEvent, cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }

            }
            // If sessionToInvalidate is null, the token was already invalid.
            // We can treat this as a success and do nothing.
        }
    }
}