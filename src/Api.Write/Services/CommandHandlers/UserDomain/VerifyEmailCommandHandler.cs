using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Abstraction;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.User;
using ProjectZenith.Contracts.Events.User;
using ProjectZenith.Contracts.Models;

namespace ProjectZenith.Api.Write.Services.Commands.UserDomain
{
    public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, UserEmailVerifiedEvent>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<VerifyEmailCommand> _validator;
        private readonly ITimeLimitedDataProtector _dataProtector;

        public VerifyEmailCommandHandler(
            WriteDbContext dbContext,
            IEventPublisher eventPublisher,
            IValidator<VerifyEmailCommand> validator,
            IDataProtectionProvider dataProtector)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _dataProtector = dataProtector.CreateProtector("EmailVerification").ToTimeLimitedDataProtector();
        }

        public async Task<UserEmailVerifiedEvent> Handle(VerifyEmailCommand command, CancellationToken cancellationToken)
        {
            //Validate the command
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            //Validate the token
            string userIsString;
            DateTimeOffset expiration;
            try
            {
                userIsString = _dataProtector.Unprotect(command.Token, out expiration);
                if (expiration < DateTimeOffset.UtcNow)
                {
                    // This check is often redundant as Unprotect already does it, but it can be explicit.
                    throw new InvalidOperationException("Token has expired.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invalid token.", ex);
            }

            if (!Guid.TryParse(userIsString, out var userId))
            {
                throw new InvalidOperationException("Invalid token format.");
            }


            //Find the user by ID
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                ?? throw new InvalidOperationException("User not found.");


            //Check if the user email is already verified
            if (user.IsEmailVerified)
            {
                throw new InvalidOperationException("Email is already verified.");
            }

            //Update user email verification status
            user.IsEmailVerified = true;

            var log = new SystemLog
            {
                UserId = userId,
                Action = "EmailVerified",
                Details = $"Email verified for user {user.Email}",
                Timestamp = DateTime.UtcNow
            };

            var userEmailVerifiedEvent = new UserEmailVerifiedEvent
            {
                UserId = user.Id,
                Email = user.Email,
                VerifiedAt = DateTime.UtcNow
            };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                //Save changes to the database
                _dbContext.Users.Update(user);
                await _dbContext.SystemLogs.AddAsync(log, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);


                await _eventPublisher.PublishAsync("user-events", userEmailVerifiedEvent, cancellationToken);

                return userEmailVerifiedEvent;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }


        }
    }
}
