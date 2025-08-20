using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Api.Write.Infrastructure.Messaging;
using ProjectZenith.Api.Write.Services.Email;
using ProjectZenith.Api.Write.Services.Security;
using ProjectZenith.Contracts.Commands.User;
using ProjectZenith.Contracts.Events.User;
using ProjectZenith.Contracts.Models;

namespace ProjectZenith.Api.Write.Services.Commands.UserDomain
{
    public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEmailService _emailService;
        private readonly IPasswordService _passwordService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<RequestPasswordResetCommand> _validator;
        private readonly ITimeLimitedDataProtector _dataProtection;

        public RequestPasswordResetCommandHandler(
            WriteDbContext dbContext,
            IEmailService emailService,
            IEventPublisher eventPublisher,
            IPasswordService passwordService,
            IValidator<RequestPasswordResetCommand> validator,
            IDataProtectionProvider dataProtectionProvider
            )
        {
            _dbContext = dbContext;
            _emailService = emailService;
            _passwordService = passwordService;
            _validator = validator;
            _dataProtection = dataProtectionProvider.CreateProtector("PasswordReset").ToTimeLimitedDataProtector();
            _eventPublisher = eventPublisher;
        }

        public async Task Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken)
        {
            var varlidationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (!varlidationResult.IsValid)
            {
                throw new ValidationException(varlidationResult.Errors);
            }

            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == command.Email);
            if (user == null)
            {
                return;
            }

            var resetToken = _dataProtection.Protect(user.Id.ToString(), TimeSpan.FromHours(1));

            // 2. Send the email with the token.
            var resetUrl = $"https://projectzenith.com/reset-password?token={Uri.EscapeDataString(resetToken)}";
            await _emailService.SendResetPasswordEmailAsync(
                user.Email,
                resetToken,
                cancellationToken);

            _dbContext.SystemLogs.Add(new SystemLog
            {
                UserId = user.Id,
                Action = "PasswordResetRequest",
                Details = $"Password reset requested for user {user.Email}",
                Timestamp = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync(cancellationToken);

            var userEvent = new PasswordResetRequestedEvent
            {
                UserId = user.Id,
                Email = user.Email,
                RequestedAt = DateTime.UtcNow
            };
            await _eventPublisher.PublishAsync("user-events", userEvent, cancellationToken);
        }
    }
}
