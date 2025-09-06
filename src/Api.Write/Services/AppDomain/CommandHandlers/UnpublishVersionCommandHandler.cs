using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Api.Write.Services.AppDomain.DomainServices;
using ProjectZenith.Contracts.Commands.App;
using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Contracts.Enums;
using ProjectZenith.Contracts.Events.App;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Infrastructure.Messaging;

namespace ProjectZenith.Api.Write.Services.AppDomain.CommandHandlers
{
    public class UnpublishVersionCommandHandler : IRequestHandler<UnpublishVersionCommand, Unit>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly IAppStatusService _appStatusService;
        private readonly IValidator<UnpublishVersionCommand> _validator;
        private readonly BlobStorageOptions _blobStorageOptions;
        private readonly ILogger<UnpublishVersionCommandHandler> _logger;

        public UnpublishVersionCommandHandler(
            WriteDbContext dbContext,
            IEventPublisher eventPublisher,
            IAppStatusService appStatusService,
            IValidator<UnpublishVersionCommand> validator,
            IOptions<BlobStorageOptions> blobStorageOptions,
            ILogger<UnpublishVersionCommandHandler> logger)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _appStatusService = appStatusService;
            _validator = validator;
            _blobStorageOptions = blobStorageOptions.Value;
            _logger = logger;
        }

        public async Task<Unit> Handle(UnpublishVersionCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            var app = await _dbContext.Apps
                .Include(a => a.Versions)
                .ThenInclude(v => v.File)
                .FirstOrDefaultAsync(a => a.Id == command.AppId && a.AppStatus == AppStatus.Active, cancellationToken)
                ?? throw new InvalidOperationException($"App {command.AppId} not found or not Active.");

            var version = app.Versions.FirstOrDefault(v => v.Id == command.VersionId && v.Status == Status.Published)
                ?? throw new InvalidOperationException($"Version {command.VersionId} not found or not in Published status.");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                //Suspersede current published version
                version.Status = Status.Superseded;

                //Find the most recent superseded version and republish it
                var previousVersion = app.Versions
                    .Where(v => v.Status == Status.Superseded)
                    .OrderByDescending(v => v.CreatedAt)
                    .FirstOrDefault();

                //If found, set it to Published
                if (previousVersion != null)
                {
                    _logger.LogInformation("Rolling back to previous version {VersionId} for app {AppId}", previousVersion.Id, app.Id);
                    previousVersion.Status = Status.Published;
                }

                await _appStatusService.UpdateAppStatusAsync(command.AppId, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var unpublishVersionEvent = new AppVersionUnpublishedEvent
                {
                    AppId = command.AppId,
                    VersionId = command.VersionId,
                    AppName = app.Name,
                    VersionNumber = version.VersionNumber,
                    UnpublishedAt = DateTime.UtcNow
                };

                await _eventPublisher.PublishAsync(
                    KafkaTopics.AppVersionUnpublishedEvents,
                    JsonSerializer.Serialize(unpublishVersionEvent),
                    cancellationToken
                );

                await transaction.CommitAsync(cancellationToken);
                return Unit.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unpublish version {VersionId} for app {AppId}: {Message}", command.VersionId, command.AppId, ex.Message);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
