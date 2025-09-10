using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.Purchase;
using ProjectZenith.Contracts.Enums;
using ProjectZenith.Contracts.Events.Purchase;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Infrastructure.Messaging;
using ProjectZenith.Contracts.Models;
using Stripe;

namespace ProjectZenith.Api.Write.Services.PurchaseDomain.CommandHandlers
{
    public class CreatePurchaseCommandHandler : IRequestHandler<CreatePurchaseCommand, Guid>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly PaymentIntentService _paymentIntentService;
        private readonly IValidator<CreatePurchaseCommand> _validator;
        private readonly ILogger<CreatePurchaseCommandHandler> _logger;

        public CreatePurchaseCommandHandler(
            WriteDbContext dbContext,
            IEventPublisher eventPublisher,
            PaymentIntentService paymentIntentService,
            IValidator<CreatePurchaseCommand> validator,
            ILogger<CreatePurchaseCommandHandler> logger)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _paymentIntentService = paymentIntentService;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreatePurchaseCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            var app = await _dbContext.Apps
                .Include(a => a.Versions)
                .FirstOrDefaultAsync(a => a.Id == command.AppId && a.AppStatus == AppStatus.Active, cancellationToken)
                ?? throw new InvalidOperationException($"App with ID {command.AppId} not found or inactive.");

            if (!app.Versions.Any(v => v.Status == Status.Published))
            {
                throw new InvalidOperationException($"App with ID {command.AppId} has no published versions.");
            }

            if (app.Price != command.Price)
            {
                throw new InvalidOperationException($"App price mismatch. Current price is {app.Price}, but command price is {command.Price}.");
            }

            var paymentIntentOptions = new PaymentIntentCreateOptions
            {
                Amount = (long)(app.Price * 100), // Convert to cents
                Currency = "usd",
                PaymentMethod = command.PaymentMethodId,
                Confirm = true,
                Description = $"Purchase of app {app.Name} (ID: {app.Id}) by user {command.UserId}"
            };

            PaymentIntent paymentIntent;
            try
            {
                paymentIntent = await _paymentIntentService.CreateAsync(paymentIntentOptions, cancellationToken: cancellationToken);
                if (paymentIntent.Status != "succeeded")
                {
                    throw new InvalidOperationException($"Payment failed with status: {paymentIntent.Status}");
                }
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error while creating payment intent for app {AppId} by user {UserId}: {Message}", command.AppId, command.UserId, ex.Message);
                throw new InvalidOperationException($"Payment processing failed, Message: {ex.Message}. Please try again.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var purchase = new Purchase
                {
                    Id = Guid.NewGuid(),
                    UserId = command.UserId,
                    AppId = command.AppId,
                    Price = command.Price,
                    Status = PurchaseStatus.Completed,
                    PurchaseDate = DateTime.UtcNow,
                };

                var transactionRecord = new Transaction
                {
                    Id = Guid.NewGuid(),
                    PurchaseId = purchase.Id,
                    Amount = command.Price,
                    PaymentProvider = command.PaymentProvider,
                    PaymentId = paymentIntent.Id,
                    Status = TransactionStatus.Completed,
                    TransactionDate = DateTime.UtcNow,
                };

                purchase.Transactions.Add(transactionRecord);
                _dbContext.Purchases.Add(purchase);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var @event = new PurchaseCompletedEvent
                {
                    PurchaseId = purchase.Id,
                    UserId = command.UserId,
                    AppId = command.AppId,
                    DeveloperId = app.DeveloperId,
                    Price = command.Price,
                    PaymentId = paymentIntent.Id,
                    CompletedAt = DateTime.UtcNow
                };

                var purchaseIdKey = @event.PurchaseId.ToString();
                await _eventPublisher.PublishAsync(KafkaTopics.Purchases, purchaseIdKey, @event, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return purchase.Id;

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error creating purchase for App ID {AppId} by User ID {UserId}, Message: {Message}", command.AppId, command.UserId, ex.Message);
                throw;
            }
        }
    }
}
