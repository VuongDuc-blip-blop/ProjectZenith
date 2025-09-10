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
using ProjectZenith.Contracts.Infrastructure.Messaging;

namespace ProjectZenith.Api.Write.Services.AppDomain.CommandHandlers
{
    public class DeleteAppCommandHandler : IRequestHandler<DeleteAppCommand, Unit>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<DeleteAppCommand> _validator;
        private readonly ILogger<DeleteAppCommandHandler> _logger;
        private readonly BlobStorageOptions _options;
        private readonly IBlobStorageService _blobStorageService;

        public DeleteAppCommandHandler(
            WriteDbContext dbContext,
            IEventPublisher eventPublisher,
            IValidator<DeleteAppCommand> validator,
            ILogger<DeleteAppCommandHandler> logger,
            IOptions<BlobStorageOptions> options,
            IBlobStorageService blobStorageService)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _logger = logger;
            _options = options.Value;
            _blobStorageService = blobStorageService;
        }

        public async Task<Unit> Handle(DeleteAppCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command);

            var app = await _dbContext.Apps
                .Include(a => a.Versions)
                    .ThenInclude(v => v.File)
                .Include(a => a.Screenshots)
                .Include(a => a.AppTags)
                .Include(a => a.Reviews)
                .Include(a => a.Purchases)
                    .ThenInclude(p => p.Transactions)
                .FirstOrDefaultAsync(a => a.Id == command.AppId && !a.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException($"App with ID {command.AppId} not found or already deleted.");

            if (app.DeveloperId != command.DeveloperId)
            {
                throw new UnauthorizedAccessException($"Developer {command.DeveloperId} do not have permission to delete app {command.AppId}.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                app.IsDeleted = true;
                app.DeletedAt = DateTime.UtcNow;

                foreach (var version in app.Versions)
                {
                    version.IsDeleted = true;
                    version.DeletedAt = DateTime.UtcNow;
                }

                foreach (var screenshot in app.Screenshots)
                {
                    screenshot.IsDeleted = true;
                    screenshot.DeletedAt = DateTime.UtcNow;
                }

                foreach (var appTag in app.AppTags)
                {
                    appTag.IsDeleted = true;
                    appTag.DeletedAt = DateTime.UtcNow;
                }

                foreach (var review in app.Reviews)
                {
                    review.IsDeleted = true;
                    review.DeletedAt = DateTime.UtcNow;
                }
                foreach (var purchase in app.Purchases)
                {
                    purchase.IsDeleted = true;
                    purchase.DeletedAt = DateTime.UtcNow;

                    foreach (var trans in purchase.Transactions)
                    {
                        trans.IsDeleted = true;
                        trans.DeletedAt = DateTime.UtcNow;
                    }
                }

                foreach (var version in app.Versions)
                {
                    var sourceContainer = DetermineSourceContainerForVersion(version.Status);
                    if (sourceContainer != null)
                    {
                        try
                        {
                            await _blobStorageService.MoveAsync(
                                sourceContainer,
                                version.File.Path,
                                _options.ArchivedContainerName,
                                version.File.Path,
                                cancellationToken
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to move blob for version {VersionId} of app {AppId}", version.Id, app.Id);
                        }
                    }
                }

                foreach (var screenshot in app.Screenshots)
                {
                    try
                    {
                        await _blobStorageService.MoveAsync(
                            _options.ValidatedContainerName,
                            screenshot.Path,
                            _options.ArchivedContainerName,
                            screenshot.Path,
                            cancellationToken
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to move blob for screenshot {ScreenshotId} of app {AppId}", screenshot.Id, app.Id);
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                var @event = new AppDeletedEvent
                {
                    AppId = app.Id,
                    DeletedAt = DateTime.UtcNow
                };

                await _eventPublisher.PublishAsync(KafkaTopics.Apps, command.AppId.ToString(), @event, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("App {AppId} deleted successfully by developer {DeveloperId}", app.Id, command.DeveloperId);
                return Unit.Value;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred while deleting app {AppId} by developer {DeveloperId}, Message: {Message}", command.AppId, command.DeveloperId, ex.Message);
                throw;
            }
        }


        private string? DetermineSourceContainerForVersion(Status versionStatus)
        {
            switch (versionStatus)
            {
                case Status.Published:
                    return _options.PublishedContainerName;
                case Status.PendingApproval:
                    return _options.ValidatedContainerName;
                default:
                    return null;
            }
        }
    }
}
