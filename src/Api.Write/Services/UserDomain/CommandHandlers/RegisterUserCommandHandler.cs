using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Api.Write.Services.UserDomain.DomainServices.Security;
using ProjectZenith.Contracts.Commands.User;
using ProjectZenith.Contracts.DTOs.User;
using ProjectZenith.Contracts.Events.User;
using ProjectZenith.Contracts.Models;
using ProjectZenith.Contracts.Infrastructure.Messaging;
using ProjectZenith.Api.Write.Services.UserDomain.DomainServices.Email;

namespace ProjectZenith.Api.Write.Services.UserDomain.CommandHandlers
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterResponseDTO>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<RegisterUserCommand> _validator;
        private readonly IPasswordService _passwordService;
        private readonly ITimeLimitedDataProtector _dataProtector;
        private readonly IEmailService _emailService;

        public RegisterUserCommandHandler(
            WriteDbContext dbContext, IEventPublisher eventPublisher, IValidator<RegisterUserCommand> validator, IPasswordService passwordService, IDataProtectionProvider dataProtectionProvider, IEmailService emailService)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _passwordService = passwordService;
            _emailService = emailService;
            _dataProtector = dataProtectionProvider.CreateProtector("EmailVerification").ToTimeLimitedDataProtector();
        }

        public async Task<RegisterResponseDTO> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
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
                CreatedAt = DateTime.UtcNow,
                IsEmailVerified = false, // Initially set to false
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

                var token = _dataProtector.Protect(userId.ToString(), TimeSpan.FromHours(24));
                await _emailService.SendVerificationEmailAsync(user.Email, token, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
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
            await _eventPublisher.PublishAsync(KafkaTopics.UserEvents, userEvent);

            RegisterResponseDTO registerResult = new RegisterResponseDTO
            {
                UserId = user.Id,
                Email = user.Email,
            };
            return registerResult;
        }

    }
}
