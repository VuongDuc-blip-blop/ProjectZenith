using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Api.Write.Infrastructure.Messaging;
using ProjectZenith.Api.Write.Services.Security;
using ProjectZenith.Contracts.Commands.User;
using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Contracts.DTOs.User;
using ProjectZenith.Contracts.Events.User;
using ProjectZenith.Contracts.Models;

namespace ProjectZenith.Api.Write.Services.CommandHandlers.DeveloperDomain
{
    public class RequestDeveloperStatusCommandHandler : IRequestHandler<RequestDeveloperStatusCommand, LoginResponseDTO>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<RequestDeveloperStatusCommand> _validator;
        private readonly DeveloperOptions _developerOptions;
        private readonly ITokenService _tokenService;

        public RequestDeveloperStatusCommandHandler(
            WriteDbContext dbContext,
            IEventPublisher eventPublisher,
            IValidator<RequestDeveloperStatusCommand> validator,
            IOptions<DeveloperOptions> developerOptions,
            ITokenService tokenService)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _developerOptions = developerOptions.Value;
            _tokenService = tokenService;
        }

        public async Task<LoginResponseDTO> Handle(RequestDeveloperStatusCommand command, CancellationToken cancellationToken)
        {
            // 1. Validate command (assuming validator is run by a pipeline or called here)
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            // 2. Check if the user exists and doesn't already have a developer profile.
            var userExists = await _dbContext.Users.AnyAsync(u => u.Id == command.UserId, cancellationToken);
            if (!userExists)
            {
                throw new InvalidOperationException($"User with ID {command.UserId} not found.");
            }

            var developerExists = await _dbContext.Developers.AnyAsync(d => d.UserId == command.UserId, cancellationToken);
            if (developerExists)
            {
                throw new InvalidOperationException("User is already a developer or has a pending request.");
            }

            // 3. Create a NEW Developer entity, respecting our schema.
            var developerProfile = new Developer
            {
                UserId = command.UserId,
                Description = command.Description,
                ContactEmail = command.ContactEmail,
                CreatedAt = DateTime.UtcNow
            };

            // We are not touching the User entity itself.
            _dbContext.Developers.Add(developerProfile);

            // 4. Determine the new role based on the approval policy.
            Role? developerRole = null;
            if (_developerOptions.ApprovalPolicy == "Auto")
            {
                // If auto-approved, we also need to assign the "Developer" role.
                developerRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Developer", cancellationToken)
                    ?? throw new InvalidOperationException("Developer role not found in the database.");

                var userRole = new UserRole { UserId = command.UserId, RoleId = developerRole.Id };
                _dbContext.UserRoles.Add(userRole);
            }

            // 5. Save changes in a transaction.
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 6. Publish the correct event(s).
            var requestedEvent = new DeveloperStatusRequestedEvent
            {
                UserId = command.UserId,
                Description = command.Description,
                ContactEmail = command.ContactEmail
            };
            await _eventPublisher.PublishAsync("developer-events", requestedEvent, cancellationToken);



            // Generate and return new tokens if auto-approved ---
            if (_developerOptions.ApprovalPolicy == "Auto")
            {
                if (developerRole != null)
                {
                    var approvedEvent = new DeveloperStatusApprovedEvent
                    {
                        UserId = command.UserId,
                        ApprovedAt = DateTime.UtcNow,
                    };
                    await _eventPublisher.PublishAsync("developer-events", approvedEvent, cancellationToken);
                }

                // Fetch the user again WITH THEIR ROLES to build the new token claims
                var userWithNewRoles = await _dbContext.Users
                    .Include(u => u.Roles)
                    .ThenInclude(ur => ur.Role)
                    .AsNoTracking() // Read-only operation
                    .SingleAsync(u => u.Id == command.UserId, cancellationToken);

                // Call the shared token service to generate a fresh set of tokens
                // The 'deviceInfo' can be null as this is a server-initiated refresh
                return await _tokenService.GenerateTokenAsync(userWithNewRoles, null, cancellationToken);
            }

            // If the policy is "Admin", the process ends here for the user.
            // We return null to indicate no new tokens were issued.
            return null;
        }
    }
}
