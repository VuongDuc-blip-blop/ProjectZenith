using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.App;
using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Contracts.Enums;
using ProjectZenith.Contracts.Events.App;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Infrastructure.MessageQueue;
using ProjectZenith.Contracts.Infrastructure.Messaging;
using ProjectZenith.Contracts.Models;

namespace ProjectZenith.Api.Write.Services.AppDomain.CommandHandlers
{
    public class SubmitNewVersionCommandHandler : IRequestHandler<SubmitNewVersionCommand, Guid>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IBlobStorageService _blobStorageService;
        private readonly IQueueService _queueService;
        private readonly IValidator<SubmitNewVersionCommand> _validator;
        private readonly BlobStorageOptions _blobStorageOptions;
        private readonly ILogger<SubmitNewVersionCommandHandler> _logger;
        private readonly IEventPublisher _eventPublisher;

        public SubmitNewVersionCommandHandler(
            WriteDbContext dbContext,
            IBlobStorageService blobStorageService,
            IQueueService queueService,
            IValidator<SubmitNewVersionCommand> validator,
            IOptions<BlobStorageOptions> blobStorageOptions,
            ILogger<SubmitNewVersionCommandHandler> logger,
            IEventPublisher eventPublisher)
        {
            _dbContext = dbContext;
            _blobStorageService = blobStorageService;
            _queueService = queueService;
            _validator = validator;
            _blobStorageOptions = blobStorageOptions.Value;
            _logger = logger;
            _eventPublisher = eventPublisher;
        }

        public async Task<Guid> Handle(SubmitNewVersionCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            var app = await _dbContext.Apps
                .FirstOrDefaultAsync(a => a.Id == command.AppId && a.DeveloperId == command.DeveloperId && a.AppStatus == AppStatus.Active, cancellationToken)
                ?? throw new InvalidOperationException($"App {command.AppId} not found or not owned by the developer {command.DeveloperId} or not active.");

            var mainAppBlobName = $"staging/{command.SubmissionId}/{command.MainAppFileName}";
            var screenshotBlobNames = command.Screenshots.Select(s => $"staging/{command.SubmissionId}/{s.FileName}").ToList();

            if (!await _blobStorageService.ExistsAsync(_blobStorageOptions.QuarantineContainerName, mainAppBlobName, cancellationToken))
            {
                throw new InvalidOperationException($"Main application file {mainAppBlobName} not found in blob storage.");
            }

            foreach (var screenshotBlobName in screenshotBlobNames)
            {
                if (!await _blobStorageService.ExistsAsync(_blobStorageOptions.QuarantineContainerName, screenshotBlobName, cancellationToken))
                {
                    throw new InvalidOperationException($"Screenshot file {screenshotBlobName} not found in blob storage.");
                }
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var AppFile = new AppFile
                {
                    Id = Guid.NewGuid(),
                    Path = command.MainAppFileName,
                    Checksum = command.MainAppChecksum,
                    Size = command.MainAppFileSize,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.AppFiles.Add(AppFile);

                var newVersion = new AppVersion
                {
                    Id = Guid.NewGuid(),
                    AppId = command.AppId,
                    VersionNumber = command.VersionNumber,
                    Changelog = command.Changelog,
                    Status = Status.PendingValidation,
                    FileId = AppFile.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.AppVersions.Add(newVersion);

                var screenshots = command.Screenshots.Select(s => new AppScreenshot
                {
                    Id = Guid.NewGuid(),
                    AppId = command.AppId,
                    Path = s.FileName,
                    Status = ScreenshotStatus.PendingValidation,
                    Size = s.Size,
                    Checksum = s.Checksum,
                    UploadedAt = DateTime.UtcNow
                }).ToList();
                _dbContext.AppScreenshots.AddRange(screenshots);

                var tagNames = command.Tags.Distinct().ToList();
                var existingTags = await _dbContext.Tags
                    .Where(t => tagNames.Contains(t.Name))
                    .ToDictionaryAsync(t => t.Name, t => t.Id, cancellationToken);

                var newTags = tagNames
                    .Where(t => !existingTags.ContainsKey(t))
                    .Select(t => new Tag { Id = Guid.NewGuid(), Name = t })
                    .ToList();

                if (newTags.Any())
                {
                    _dbContext.Tags.AddRange(newTags);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    foreach (var tag in newTags)
                    {
                        existingTags[tag.Name] = tag.Id;
                    }
                }

                var appTags = tagNames.Select(t => new AppTag
                {
                    AppId = command.AppId,
                    TagId = existingTags[t]
                }).ToList();
                _dbContext.AppTags.AddRange(appTags);

                await _dbContext.SaveChangesAsync(cancellationToken);

                var appFileMetadata = new Dictionary<string, string>
                {
                    { "AppId", command.AppId.ToString() },
                    { "AppFileId", AppFile.Id.ToString() },
                    { "VersionId", newVersion.Id.ToString() }, // Added for version-specific handling
                    { "DeveloperId", command.DeveloperId.ToString() },
                    { "AppName", SanitizeForUrl(app.Name) },
                    { "VersionNumber", command.VersionNumber },
                    { "Checksum", command.MainAppChecksum }
                };
                await _blobStorageService.SetBlobMetadataAsync(_blobStorageOptions.QuarantineContainerName, mainAppBlobName, appFileMetadata, cancellationToken);

                for (int i = 0; i < screenshots.Count; i++)
                {
                    var screenshotMetadata = new Dictionary<string, string>
                    {
                        { "AppId", command.AppId.ToString() },
                        { "ScreenshotId", screenshots[i].Id.ToString() },
                        { "Checksum", command.Screenshots[i].Checksum }
                    };

                    await _blobStorageService.SetBlobMetadataAsync(_blobStorageOptions.QuarantineContainerName, screenshotBlobNames[i], screenshotMetadata, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                await _queueService.SendMessageAsync(_blobStorageOptions.AppFileQueue, mainAppBlobName, cancellationToken);
                foreach (var screenshotBlobName in screenshotBlobNames)
                {
                    await _queueService.SendMessageAsync(_blobStorageOptions.ScreenshotQueue, screenshotBlobName, cancellationToken);
                }

                var @event = new AppVersionSubmittedEvent
                {
                    AppId = command.AppId,
                    VersionId = newVersion.Id,
                    DeveloperId = command.DeveloperId,
                    VersionNumber = command.VersionNumber,
                    SubmittedAt = DateTime.UtcNow
                };
                var appIdKey = @event.AppId.ToString();
                await _eventPublisher.PublishAsync(KafkaTopics.Apps, appIdKey, @event, cancellationToken);

                return newVersion.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit new version for app {AppId}: {Message}", command.AppId, ex.Message);
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