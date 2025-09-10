using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.Developer;
using ProjectZenith.Contracts.Enums;
using ProjectZenith.Contracts.Events.Developer;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Infrastructure.Messaging;
using Stripe;

namespace ProjectZenith.Api.Write.Services.DeveloperDomain.CommandHandlers
{
    public class CreateStripeConnectOnboardingLinkCommandHandler : IRequestHandler<CreateStripeConnectOnboardingLinkCommand, string>
    {
        private readonly WriteDbContext _dbContext;
        private readonly AccountService _accountService;
        private readonly AccountLinkService _accountLinkService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<CreateStripeConnectOnboardingLinkCommand> _validator;
        private readonly ILogger<CreateStripeConnectOnboardingLinkCommandHandler> _logger;

        public CreateStripeConnectOnboardingLinkCommandHandler(
            WriteDbContext dbContext,
            AccountService accountService,
            AccountLinkService accountLinkService,
            IEventPublisher eventPublisher,
            ILogger<CreateStripeConnectOnboardingLinkCommandHandler> logger,
            IValidator<CreateStripeConnectOnboardingLinkCommand> validator)
        {
            _dbContext = dbContext;
            _accountService = accountService;
            _accountLinkService = accountLinkService;
            _eventPublisher = eventPublisher;
            _logger = logger;
            _validator = validator;
        }

        public async Task<string> Handle(CreateStripeConnectOnboardingLinkCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            var developer = await _dbContext.Developers
                .FirstOrDefaultAsync(d => d.UserId == command.DeveloperId, cancellationToken)
                ?? throw new InvalidOperationException($"Developer {command.DeveloperId} not found.");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                string stripeAccountId;
                if (string.IsNullOrEmpty(developer.StripeAccountId))
                {
                    var accountOptions = new AccountCreateOptions
                    {
                        Type = "express",
                        Country = "US",
                        Capabilities = new AccountCapabilitiesOptions
                        {
                            Transfers = new AccountCapabilitiesTransfersOptions
                            {
                                Requested = true,
                            },
                        }
                    };

                    var account = await _accountService.CreateAsync(accountOptions, null, cancellationToken);
                    developer.StripeAccountId = account.Id;
                    developer.PayoutStatus = DeveloperPayoutStatus.OnBoardingInProgress;

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    stripeAccountId = account.Id;

                    _logger.LogInformation("Created new Stripe account {StripeAccountId} for developer {DeveloperId}.", stripeAccountId, developer.UserId);
                }
                else
                {
                    stripeAccountId = developer.StripeAccountId;
                    _logger.LogInformation("Using existing Stripe account {StripeAccountId} for developer {DeveloperId}.", stripeAccountId, developer.UserId);
                }

                var accountLinkOptions = new AccountLinkCreateOptions
                {
                    Account = stripeAccountId,
                    RefreshUrl = command.RefreshUrl,
                    ReturnUrl = command.ReturnUrl,
                    Type = "account_onboarding",
                };

                var accountLink = await _accountLinkService.CreateAsync(accountLinkOptions, null, cancellationToken);

                var @event = new PayoutOnboardingStartedEvent(
                    DeveloperId: command.DeveloperId,
                    StripeAccountId: stripeAccountId,
                    StartedAt: DateTime.UtcNow
                );

                await _eventPublisher.PublishAsync(KafkaTopics.Payouts, developer.UserId.ToString(), @event, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Created Stripe onboarding link for developer {DeveloperId}.", developer.UserId);

                return accountLink.Url;
            }
            catch (StripeException stripeEx)
            {
                _logger.LogError(stripeEx, "Stripe error creating onboarding link for DeveloperId {DeveloperId}, Message: {Message}", command.DeveloperId, stripeEx.Message);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error creating onboarding link for DeveloperId {DeveloperId}, Message: {Message}", command.DeveloperId, ex.Message);
                throw;
            }
        }
    }
}
