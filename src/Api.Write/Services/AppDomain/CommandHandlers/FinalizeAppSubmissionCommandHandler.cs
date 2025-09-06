using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.App;
using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Contracts.Enums;
using ProjectZenith.Contracts.Events.User;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Infrastructure.MessageQueue;
using ProjectZenith.Contracts.Infrastructure.Messaging;
using ProjectZenith.Contracts.Models;

namespace ProjectZenith.Api.Write.Services.AppDomain.CommandHandlers
{
    public class FinalizeAppSubmissionCommandHandler : IRequestHandler<FinalizeAppSubmissionCommand, Guid>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IBlobStorageService _blobService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<FinalizeAppSubmissionCommand> _validator;
        private readonly IQueueService _queueService;
        private readonly BlobStorageOptions _blobStorageOptions;
        private readonly ILogger<FinalizeAppSubmissionCommandHandler> _logger;

        public FinalizeAppSubmissionCommandHandler(
            WriteDbContext dbContext,
            IBlobStorageService blobService,
            IEventPublisher eventPublisher,
            IValidator<FinalizeAppSubmissionCommand> validator,
            IQueueService queueService,
            IOptions<BlobStorageOptions> blobStorageOptions,
            ILogger<FinalizeAppSubmissionCommandHandler> logger)
        {
            _dbContext = dbContext;
            _blobService = blobService;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _queueService = queueService;
            _blobStorageOptions = blobStorageOptions.Value;
            _logger = logger;
        }


        public async Task<Guid> Handle(
            FinalizeAppSubmissionCommand command,
            CancellationToken cancellationToken)
        {

            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            var developerExists = await _dbContext.Developers.AnyAsync(u => u.UserId == command.DeveloperId, cancellationToken);
            if (!developerExists)
            {
                throw new InvalidOperationException($"Developer with ID {command.DeveloperId} not found.");
            }
            // Construct blob paths
            var mainAppBlobName = $"staging/{command.SubmissionId}/{command.MainAppFileName}";
            var screenshotBlobNames = command.Screenshots.Select(s => $"staging/{command.SubmissionId}/{s.FileName}").ToList();


            // Verify main app file exists
            var blobExists = await _blobService.ExistsAsync(
                _blobStorageOptions.QuarantineContainerName,
                mainAppBlobName,
                cancellationToken);

            if (!blobExists)
                throw new InvalidOperationException("File not found in storage.");

            // Verify screenshot files exist
            foreach (var screenshotBlobName in screenshotBlobNames)
            {
                var exists = await _blobService.ExistsAsync(_blobStorageOptions.QuarantineContainerName, screenshotBlobName, cancellationToken);
                if (!exists)
                {
                    throw new InvalidOperationException($"Screenshot file {screenshotBlobName} not found in storage.");
                }
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var newFile = new AppFile
                {
                    Id = Guid.NewGuid(),
                    Path = command.MainAppFileName,
                    Size = command.MainAppFileSize,
                    Checksum = command.MainAppChecksum,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.AppFiles.Add(newFile);

                var app = new App
                {
                    Id = Guid.NewGuid(),
                    DeveloperId = command.DeveloperId,
                    Name = command.AppName,
                    Description = command.Description,
                    Category = command.Category,
                    Platform = command.Platform,
                    Price = command.Price,

                };

                var version = new AppVersion
                {
                    Id = Guid.NewGuid(),
                    AppId = app.Id,
                    VersionNumber = command.VersionNumber,
                    Changelog = command.Changelog,
                    FileId = newFile.Id,
                    Status = Status.PendingValidation,
                };

                _dbContext.Apps.Add(app);
                _dbContext.AppVersions.Add(version);

                var screenshots = command.Screenshots.Select(s => new AppScreenshot
                {
                    Id = Guid.NewGuid(),
                    AppId = app.Id,
                    Path = $"staging/{command.SubmissionId}/{s.FileName}",
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
                    foreach (var tag in newTags)
                    {
                        existingTags[tag.Name] = tag.Id;
                    }
                }

                var appTags = tagNames.Select(tn => new AppTag
                {
                    AppId = app.Id,
                    TagId = existingTags[tn]
                }).ToList();

                _dbContext.AppTags.AddRange(appTags);


                await _dbContext.SaveChangesAsync(cancellationToken);

                var appFileMetadata = new Dictionary<string, string>
                {
                    { "AppId", app.Id.ToString() },
                    { "AppFileId", newFile.Id.ToString() },
                    { "DeveloperId", app.DeveloperId.ToString() },
                    {"AppName",command.AppName },
                    { "VersionNumber", command.VersionNumber },
                    { "Checksum", command.MainAppChecksum}
                };

                await _blobService.SetBlobMetadataAsync(_blobStorageOptions.QuarantineContainerName, mainAppBlobName, appFileMetadata, cancellationToken);

                for (int i = 0; i < screenshots.Count; i++)
                {
                    var screenshotMetadata = new Dictionary<string, string>
                    {
                        { "AppId", app.Id.ToString() },
                        { "ScreenshotId", screenshots[i].Id.ToString() },
                        { "Checksum", command.Screenshots[i].Checksum }
                    };

                    await _blobService.SetBlobMetadataAsync(_blobStorageOptions.QuarantineContainerName, screenshots[i].Path, screenshotMetadata, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                try
                {
                    var appEvent = new AppSubmittedEvent
                    {
                        AppId = app.Id,
                        DeveloperId = command.DeveloperId,
                        AppName = command.AppName,
                        Description = command.Description,
                        Category = command.Category,
                        Platform = command.Platform,
                        Price = command.Price,
                        Version = command.VersionNumber,
                        SubmittedAt = DateTime.Now,
                    };
                    await _eventPublisher.PublishAsync(KafkaTopics.AppEvents, appEvent, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish AppSubmittedEvent for app {AppId}: {Message}", app.Id, ex.Message);
                    throw;
                }

                // Send main app file to app file queue on Azure
                try
                {
                    await _queueService.SendMessageAsync(_blobStorageOptions.AppFileQueue, mainAppBlobName, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to queue app file '{BlobName}' for processing.", mainAppBlobName);
                    throw;
                }

                // Send screenshots to screenshot queue on Azure
                try
                {
                    foreach (var screenshotBlobName in screenshotBlobNames)
                    {
                        await _queueService.SendMessageAsync(_blobStorageOptions.ScreenshotQueue, screenshotBlobName, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to queue screenshot '{BlobName}' for processing.", screenshotBlobNames);
                    throw;
                }

                return app.Id;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
