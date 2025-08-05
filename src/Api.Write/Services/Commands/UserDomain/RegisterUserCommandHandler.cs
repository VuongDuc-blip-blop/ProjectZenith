using FluentValidation;
using ProjectZenith.Api.Write.Abstraction;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Api.Write.Services.Security;
using ProjectZenith.Contracts.Commands;
using ProjectZenith.Contracts.Events;
using ProjectZenith.Contracts.Models;

namespace ProjectZenith.Api.Write.Services.Commands.UserDomain
{
    public class RegisterUserCommandHandler
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<RegisterUserCommand> _validator;
        private readonly IPasswordService _passwordService;

        public RegisterUserCommandHandler(
            WriteDbContext dbContext, IEventPublisher eventPublisher, IValidator<RegisterUserCommand> validator, IPasswordService passwordService)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _passwordService = passwordService;
        }

        public async Task<UserRegisteredEvent> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken)
        {
            // Validate the command
            var validationResult = await _validator.ValidateAsync(command);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
            // Create a new user entity
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = command.Email,
                Username = command.Username,
                CreatedAt = DateTime.UtcNow
            };

            //Create credential entity
            var credential = new Credential
            {
                UserId = userId,
                PasswordHash = _passwordService.HashPassword(command.Password),
                CreatedAt = DateTime.UtcNow
            };

            //Assign default "User" role
            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = Guid.Parse("00000000-0000-0000-0000-000000000001") // Default User role
            };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Add user, credential, and role to the database
                await _dbContext.Users.AddAsync(user, cancellationToken);
                await _dbContext.Credentials.AddAsync(credential, cancellationToken);
                await _dbContext.UserRoles.AddAsync(userRole, cancellationToken);
                // Commit the transaction
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                // Rollback the transaction in case of an error
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            var userEvent = new UserRegisteredEvent
            {
                UserId = user.Id,
                Email = user.Email,
                Username = user.Username,
                RegisteredAt = DateTime.UtcNow
            };
            // Publish an event after successful registration
            await _eventPublisher.PublishAsync("user-events", userEvent);

            return userEvent;
        }

    }
}
