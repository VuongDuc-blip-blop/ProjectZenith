using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Api.Write.Services.UserDomain.DomainServices.Security;
using ProjectZenith.Contracts.Commands.User;
using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Contracts.DTOs.User;
using ProjectZenith.Contracts.Events.User;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Infrastructure.Messaging;
using ProjectZenith.Contracts.Models;
namespace ProjectZenith.Api.Write.Services.UserDomain.CommandHandlers
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDTO>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IPasswordService _passwordService;
        private readonly IEventPublisher _eventPublisher;
        private readonly JwtOptions _jwtOptions;
        private readonly IValidator<LoginCommand> _validator;
        private readonly ITokenService _tokenService;

        public LoginCommandHandler(
            WriteDbContext dbContext,
            IPasswordService passwordService,
            IEventPublisher eventPublisher,
            IOptions<JwtOptions> jwtOptions,
            IValidator<LoginCommand> validator,
            ITokenService tokenService)
        {
            _dbContext = dbContext;
            _passwordService = passwordService;
            _eventPublisher = eventPublisher;
            _jwtOptions = jwtOptions.Value;
            _validator = validator;
            _tokenService = tokenService;
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

            var LoginResult = await _tokenService.GenerateTokenAsync(user, command.DeviceInfo, cancellationToken);


            var userEvent = new UserLoggedInEvent
            {
                UserId = user.Id,
                Email = user.Email,
                LoggedInAt = DateTime.UtcNow
            };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                _dbContext.SystemLogs.Add(new SystemLog
                {
                    UserId = user.Id,
                    Action = "LoginSuccess",
                    Details = $"User {user.Email} logged in from device {command.DeviceInfo ?? "unknown"}",
                    Timestamp = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync(cancellationToken);


                await _eventPublisher.PublishAsync(KafkaTopics.UserEvents, userEvent, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }


            return LoginResult;

        }
    }
}
