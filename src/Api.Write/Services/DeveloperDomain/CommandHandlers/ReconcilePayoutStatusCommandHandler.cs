using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.Developer;
using ProjectZenith.Contracts.Enums;
using ProjectZenith.Contracts.Events.Developer;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Infrastructure.MessageQueue;
using ProjectZenith.Contracts.Infrastructure.Messaging;
using Stripe;

namespace ProjectZenith.Api.Write.Services.DeveloperDomain.CommandHandlers
{
    public class ReconcilePayoutStatusCommandHandler : IRequestHandler<ReconcilePayoutStatusCommand, Unit>
    {
        private readonly WriteDbContext _dbContext;
        private readonly AccountService _accountService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<ReconcilePayoutStatusCommand> _validator;
        private readonly ILogger<ReconcilePayoutStatusCommandHandler> _logger;

        public ReconcilePayoutStatusCommandHandler(
            WriteDbContext dbContext,
            AccountService accountService,
            IEventPublisher eventPublisher,
            IValidator<ReconcilePayoutStatusCommand> validator,
            ILogger<ReconcilePayoutStatusCommandHandler> logger)
        {
            _dbContext = dbContext;
            _accountService = accountService;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Unit> Handle(ReconcilePayoutStatusCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            var developer = await _dbContext.Developers
                .FirstOrDefaultAsync(d => d.UserId == command.DeveloperId, cancellationToken)
                ?? throw new InvalidOperationException($"Developer {command.DeveloperId} not found.");

            if (string.IsNullOrEmpty(developer.StripeAccountId))
            {
                _logger.LogWarning("Developer {DeveloperId} has no StripeAccountId, cannot reconcile payout status.", developer.UserId);
                return Unit.Value;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var account = await _accountService.GetAsync(developer.StripeAccountId, null, cancellationToken: cancellationToken);
                var newStatus = DeterminePayoutStatus(account);

                if (developer.PayoutStatus != newStatus)
                {
                    developer.PayoutStatus = newStatus;
                    _dbContext.Developers.Update(developer);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Reconciled payout status for Developer {DeveloperId} to {PayoutStatus}.", developer.UserId, developer.PayoutStatus);

                    var @event = new PayoutOnboardingCompletedEvent
                    {
                        DeveloperId = developer.UserId,
                        StripeAccountId = developer.StripeAccountId,
                        PayoutStatus = newStatus,
                        CompletedAt = DateTime.UtcNow
                    };

                    await _eventPublisher.PublishAsync(KafkaTopics.Developers, developer.UserId.ToString(), @event, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                return Unit.Value;
            }
            catch (StripeException stripeEx)
            {
                _logger.LogError(stripeEx, "Stripe error reconciling payout status for Developer {DeveloperId}, Message: {Message}", developer.UserId, stripeEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reconciling payout status for Developer {DeveloperId}: {Message}", developer.UserId, ex.Message);
                throw;
            }
        }

        private DeveloperPayoutStatus DeterminePayoutStatus(Account account)
        {
            if (account.PayoutsEnabled && account.ChargesEnabled)
            {
                return DeveloperPayoutStatus.Enabled;
            }

            if (account.DetailsSubmitted)
            {
                // Already submitted details but payouts or charges are restricted
                return DeveloperPayoutStatus.OnboardedButRestricted;
            }

            // Default case
            return DeveloperPayoutStatus.OnBoardingInProgress;
        }
    }


}
