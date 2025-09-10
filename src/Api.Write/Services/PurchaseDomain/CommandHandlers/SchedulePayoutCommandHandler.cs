using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.Purchase;
using ProjectZenith.Contracts.Events.Purchase;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Infrastructure.Messaging;
using ProjectZenith.Contracts.Models;

namespace ProjectZenith.Api.Write.Services.PurchaseDomain.CommandHandlers
{
    public class SchedulePayoutCommandHandler : IRequestHandler<SchedulePayoutCommand, Guid>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<SchedulePayoutCommand> _validator;
        private readonly ILogger<SchedulePayoutCommandHandler> _logger;

        public SchedulePayoutCommandHandler(
            WriteDbContext dbContext,
            IEventPublisher eventPublisher,
            IValidator<SchedulePayoutCommand> validator,
            ILogger<SchedulePayoutCommandHandler> logger)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Guid> Handle(SchedulePayoutCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            var purchase = await _dbContext.Purchases
                .Include(p => p.App)
                .ThenInclude(a => a.Developer)
                .FirstOrDefaultAsync(p => p.Id == command.PurchaseId && p.Status == PurchaseStatus.Completed, cancellationToken)
                ?? throw new InvalidOperationException($"Purchase {command.PurchaseId} not found or not completed.");

            if (purchase.App.DeveloperId != command.DeveloperId)
            {
                throw new UnauthorizedAccessException($"Developer {command.DeveloperId} does not own the app associated with this purchase.");
            }

            var expectedAmount = purchase.Price * 0.7m; // Assuming a 70% payout rate, 30% platform fee

            if (Math.Abs(command.Amount - expectedAmount) > 0.01m)
            {
                throw new InvalidOperationException($"Payout amount {command.Amount} does not match expected amount {expectedAmount}.");
            }

            if (string.IsNullOrEmpty(purchase.App.Developer.StripeAccountId))
            {
                throw new InvalidOperationException($"Developer {command.DeveloperId} does not have a valid Stripe account ID.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var payout = new Payout
                {
                    Id = Guid.NewGuid(),
                    DeveloperId = command.DeveloperId,
                    Amount = command.Amount,
                    Status = PayoutStatus.Scheduled,
                    ScheduledAt = DateTime.UtcNow,
                    ProcessAt = DateTime.UtcNow.AddDays(30), // Process payout after 30 days
                    PaymentProvider = command.PaymentProvider,
                    PaymentId = command.PaymentId,
                };

                _dbContext.Payouts.Add(payout);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var @event = new PayoutScheduledEvent
                {
                    PayoutId = payout.Id,
                    DeveloperId = command.DeveloperId,
                    Amount = command.Amount,
                    ScheduledAt = payout.ScheduledAt,
                    ProcessAt = payout.ProcessAt!.Value
                };

                var payoutIdKey = @event.PayoutId.ToString();
                await _eventPublisher.PublishAsync(KafkaTopics.Payouts, payoutIdKey, @event, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return payout.Id;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error scheduling payout for PurchaseId {PurchaseId} and DeveloperId {DeveloperId}, Message: {Message}", command.PurchaseId, command.DeveloperId, ex.Message);
                throw;
            }
        }
    }
}
