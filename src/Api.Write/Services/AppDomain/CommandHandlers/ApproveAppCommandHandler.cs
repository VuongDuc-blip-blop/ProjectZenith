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
    public class ApproveAppCommandHandler : IRequestHandler<ApproveAppCommand, Unit>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IBlobStorageService _blobStorageService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IAppStatusService _appStatusService;
        private readonly IValidator<ApproveAppCommand> _validator;
        private readonly BlobStorageOptions _blobStorageOptions;
        private readonly ILogger<ApproveAppCommandHandler> _logger;


        public ApproveAppCommandHandler(WriteDbContext dbContext,
            IBlobStorageService blobStorageService,
            IEventPublisher eventPublisher,
            IValidator<ApproveAppCommand> validator,
            IAppStatusService appStatusService,
            IOptions<BlobStorageOptions> blobStorageOptions,
            ILogger<ApproveAppCommandHandler> logger)
        {
            _dbContext = dbContext;
            _blobStorageService = blobStorageService;
            _eventPublisher = eventPublisher;
            _appStatusService = appStatusService;
            _validator = validator;
            _blobStorageOptions = blobStorageOptions.Value;
            _logger = logger;
        }

        public async Task<Unit> Handle(ApproveAppCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            var app = await _dbContext.Apps
            .Include(a => a.Versions)
            .ThenInclude(v => v.File)
            .Include(a => a.Screenshots)
            .FirstOrDefaultAsync(a => a.Id == command.AppId && a.AppStatus == AppStatus.Active, cancellationToken)
            ?? throw new InvalidOperationException($"App {command.AppId} not found or not Active.");

            var version = app.Versions.FirstOrDefault(v => v.Id == command.VersionId && v.Status == Status.PendingApproval)
            ?? throw new InvalidOperationException($"Version {command.VersionId} not found or not in PendingApproval status.");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var previousPublishedVersion = app.Versions.FirstOrDefault(v => v.Status == Status.Published);
                if (previousPublishedVersion != null)
                {
                    previousPublishedVersion.Status = Status.Superseded;
                }

                version.Status = Status.Published;
                app.UpdatedAt = DateTime.UtcNow;

                var finalAppBlobName = $"{app.DeveloperId}/{SanitizeForUrl(app.Name)}/{version.VersionNumber}/{Path.GetFileName(version.File.Path)}";

                await _blobStorageService.MoveAsync(
                    _blobStorageOptions.QuarantineContainerName,
                    version.File.Path,
                    _blobStorageOptions.PublishedContainerName,
                    finalAppBlobName,
                    cancellationToken
                );

                foreach (var screenshot in app.Screenshots)
                {
                    var finalScreenshotBlobName = $"{app.DeveloperId}/{SanitizeForUrl(app.Name)}/screenshots/{screenshot.Id}{Path.GetExtension(screenshot.Path)}";
                    await _blobStorageService.MoveAsync(
                        _blobStorageOptions.QuarantineContainerName,
                        screenshot.Path,
                        _blobStorageOptions.PublishedContainerName,
                        finalScreenshotBlobName,
                        cancellationToken
                    );
                    screenshot.Path = finalScreenshotBlobName;
                }

                await _appStatusService.UpdateAppStatusAsync(command.AppId, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                try
                {
                    var @event = new AppVersionApprovedEvent
                    {
                        AppId = app.Id,
                        VersionId = version.Id,
                        DeveloperId = app.DeveloperId,
                        AppName = app.Name,
                        VersionNumber = version.VersionNumber,
                        ApprovedAt = DateTime.UtcNow
                    };

                    var appIdKey = @event.AppId.ToString();
                    await _eventPublisher.PublishAsync(KafkaTopics.Apps, appIdKey, @event, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish AppApprovedEvent for App ID {AppId}: {Message}", app.Id, ex.Message);
                    throw;
                }

                return Unit.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error approving app with ID {AppId}. Rolling back transaction. Message:{Message}", command.AppId, ex.Message);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private string SanitizeForUrl(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input.ToLowerInvariant(), @"[^a-z0-9\-]", "-").Trim('-');
        }
    }
}