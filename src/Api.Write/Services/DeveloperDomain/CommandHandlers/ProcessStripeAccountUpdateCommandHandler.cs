using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.Developer;
using ProjectZenith.Contracts.Enums;
using ProjectZenith.Contracts.Events.Developer;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Infrastructure.Messaging;

namespace ProjectZenith.Api.Write.Services.DeveloperDomain.CommandHandlers
{
    public class ProcessStripeAccountUpdateCommandHandler : IRequestHandler<ProcessStripeAccountUpdateCommand, Unit>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<ProcessStripeAccountUpdateCommand> _validator;
        private readonly ILogger<ProcessStripeAccountUpdateCommandHandler> _logger;

        public ProcessStripeAccountUpdateCommandHandler(
            WriteDbContext dbContext,
            IEventPublisher eventPublisher,
            IValidator<ProcessStripeAccountUpdateCommand> validator,
            ILogger<ProcessStripeAccountUpdateCommandHandler> logger)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Unit> Handle(ProcessStripeAccountUpdateCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            var developer = await _dbContext.Developers
                .FirstOrDefaultAsync(d => d.StripeAccountId == command.StripeAccountId, cancellationToken)
                ?? throw new InvalidOperationException($"Developer with Stripe Account ID {command.StripeAccountId} not found.");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var newStatus = DeterminePayoutStatus(command);
                if (developer.PayoutStatus != newStatus)
                {
                    developer.PayoutStatus = newStatus;
                    _dbContext.Developers.Update(developer);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Updated payout status for Developer {DeveloperId} to {PayoutStatus}.", developer.UserId, developer.PayoutStatus);

                    var @event = new PayoutOnboardingCompletedEvent
                    {
                        DeveloperId = developer.UserId,
                        StripeAccountId = command.StripeAccountId,
                        PayoutStatus = newStatus,
                        CompletedAt = DateTime.UtcNow
                    };

                    await _eventPublisher.PublishAsync(KafkaTopics.Developers, developer.UserId.ToString(), @event, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Processed Stripe account update for Developer {DeveloperId} with Status {PayoutStatus}.", developer.UserId, developer.PayoutStatus);

                return Unit.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe account update for Developer with Stripe Account ID {StripeAccountId}: {Message}", command.StripeAccountId, ex.Message);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private DeveloperPayoutStatus DeterminePayoutStatus(ProcessStripeAccountUpdateCommand command)
        {
            if (command.PayoutsEnabled && command.ChargesEnabled)
            {
                return DeveloperPayoutStatus.Enabled;
            }

            if (command.DetailsSubmitted)
            {
                // Already submitted details but payouts or charges are restricted
                return DeveloperPayoutStatus.OnboardedButRestricted;
            }

            // Default case
            return DeveloperPayoutStatus.OnBoardingInProgress;
        }
    }
}
