using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Api.Write.Services.AppDomain.DomainServices;
using ProjectZenith.Contracts.Commands.App;
using ProjectZenith.Contracts.Enums;
using ProjectZenith.Contracts.Events.App;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Infrastructure.Messaging;

namespace ProjectZenith.Api.Write.Services.AppDomain.CommandHandlers
{
    public class SetAppPriceCommandHandler : IRequestHandler<SetAppPriceCommand, Unit>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly IAppStatusService _appStatusService;
        private readonly IValidator<SetAppPriceCommand> _validator;
        private readonly ILogger<SetAppPriceCommandHandler> _logger;

        public SetAppPriceCommandHandler(
            WriteDbContext dbContext,
            IEventPublisher eventPublisher,
            IAppStatusService appStatusService,
            IValidator<SetAppPriceCommand> validator,
            ILogger<SetAppPriceCommandHandler> logger)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _appStatusService = appStatusService;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Unit> Handle(SetAppPriceCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            var app = await _dbContext.Apps
                .FirstOrDefaultAsync(a => a.Id == command.AppId && a.DeveloperId == command.DeveloperId && a.AppStatus == AppStatus.Active, cancellationToken)
                ?? throw new InvalidOperationException($"App with ID {command.AppId} not found or not owned by {command.DeveloperId} or not active.");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                app.Price = command.Price;
                app.UpdatedAt = DateTime.UtcNow;

                await _appStatusService.UpdateAppStatusAsync(command.AppId, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var @event = new AppPriceUpdatedEvent
                {
                    DeveloperId = command.DeveloperId,
                    AppId = command.AppId,
                    NewPrice = command.Price,
                    UpdatedAt = DateTime.UtcNow
                };

                var appIdKey = @event.AppId.ToString();
                await _eventPublisher.PublishAsync(KafkaTopics.Apps, appIdKey, @event, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return Unit.Value;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error setting price for App ID {AppId} by Developer ID {DeveloperId}", command.AppId, command.DeveloperId);
                throw;
            }
        }
    }
}
