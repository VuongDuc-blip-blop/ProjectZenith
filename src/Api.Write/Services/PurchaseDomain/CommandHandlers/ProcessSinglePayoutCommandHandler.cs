using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.Purchase;
using ProjectZenith.Contracts.Events.Purchase;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Infrastructure.Messaging;
using ProjectZenith.Contracts.Models;
using Stripe;

namespace ProjectZenith.Api.Write.Services.PurchaseDomain.CommandHandlers
{
    public class ProcessSinglePayoutCommandHandler : IRequestHandler<ProcessSinglePayoutCommand, Unit>
    {
        private readonly WriteDbContext _dbContext;
        private readonly TransferService _transferService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<ProcessSinglePayoutCommand> _validator;
        private readonly ILogger<ProcessSinglePayoutCommandHandler> _logger;

        public ProcessSinglePayoutCommandHandler(
            WriteDbContext dbContext,
            TransferService transferService,
            IEventPublisher eventPublisher,
            IValidator<ProcessSinglePayoutCommand> validator,
            ILogger<ProcessSinglePayoutCommandHandler> logger)
        {
            _dbContext = dbContext;
            _transferService = transferService;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Unit> Handle(ProcessSinglePayoutCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            var payout = await _dbContext.Payouts
                .Include(p => p.Developer)
                .FirstOrDefaultAsync(p => p.Id == command.PayoutId && p.Status == PayoutStatus.Scheduled, cancellationToken)
                ?? throw new InvalidOperationException($"Payout {command.PayoutId} not found or not scheduled.");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var transferOptions = new TransferCreateOptions
                {
                    Amount = (long)(payout.Amount * 100), // Convert to cents
                    Currency = "usd",
                    Destination = payout.Developer.StripeAccountId,
                    Description = $"Payout for Developer {payout.DeveloperId}, Payout Payment ID: {payout.PaymentId}"
                };

                var transfer = await _transferService.CreateAsync(transferOptions, null, cancellationToken);

                payout.Status = PayoutStatus.Processed;
                payout.CompletedAt = DateTime.UtcNow;
                payout.PaymentId = transfer.Id;

                await _dbContext.SaveChangesAsync(cancellationToken);

                var @event = new PayoutProcessedEvent
                {
                    PayoutId = payout.Id,
                    DeveloperId = payout.DeveloperId,
                    Amount = payout.Amount,
                    PaymentId = transfer.Id,
                    ProcessedAt = DateTime.UtcNow
                };

                var payoutIdKey = @event.PayoutId.ToString();
                await _eventPublisher.PublishAsync(KafkaTopics.Payouts, payoutIdKey, @event, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return Unit.Value;
            }
            catch (StripeException stripeEx)
            {
                _logger.LogError(stripeEx, "Failed to process payout {PayoutId}: {Message}", command.PayoutId, stripeEx.Message);
                payout.Status = PayoutStatus.Failed;
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken); // Commit to record Failed status
                return Unit.Value; // Allow retry via ProcessPayoutsFunction
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error processing payout {PayoutId}, Message: {Message}", command.PayoutId, ex.Message);
                throw;
            }
        }
    }
}
