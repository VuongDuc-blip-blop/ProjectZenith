using System.Security.Cryptography;
using System.Threading.Tasks;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Contracts.Events.App;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Infrastructure.Messaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace FileScan.Functions;

public class ProcessScreenshot
{
    private readonly ILogger<ProcessScreenshot> _logger;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IEventPublisher _eventPublisher;
    private readonly BlobStorageOptions _blobStorageOptions;
    private readonly KafkaOptions _kafkaOptions;

    public ProcessScreenshot(ILogger<ProcessScreenshot> logger,
        IBlobStorageService blobStorageService,
        IEventPublisher eventPublisher,
        IOptions<BlobStorageOptions> blobStorageOptions,
        IOptions<KafkaOptions> kafkaOptions)
    {
        _logger = logger;
        _blobStorageService = blobStorageService;
        _eventPublisher = eventPublisher;
        _blobStorageOptions = blobStorageOptions.Value;
        _kafkaOptions = kafkaOptions.Value;
    }

    [Function(nameof(ProcessScreenshot))]
    public async Task Run([QueueTrigger("screenshots-pending-validation", Connection = "AzureWebJobsStorage")] string blobName, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing screenshot '{BlobName}' from container '{ContainerName}'.", blobName, _blobStorageOptions.QuarantineContainerName);

        var blobProperties = await _blobStorageService.GetPropertiesAsync(_blobStorageOptions.QuarantineContainerName, blobName, cancellationToken);
        if (blobProperties == null)
        {
            _logger.LogError("Screenshot '{BlobName}' not found in container '{ContainerName}'.", blobName, _blobStorageOptions.QuarantineContainerName);
            await DeleteInvalidBlobAsync(blobName, "Screenshot not found", cancellationToken);
            return;
        }

        var metadata = blobProperties.Metadata;

        if (!metadata.TryGetValue("AppId", out var appIdStr) || !Guid.TryParse(appIdStr, out var appId) ||
            !metadata.TryGetValue("ScreenshotId", out var screenshotIdStr) || !Guid.TryParse(screenshotIdStr, out var screenshotId))
        {
            _logger.LogError("Invalid or missing metadata for screenshot '{BlobName}'.", blobName);
            await DeleteInvalidBlobAsync(blobName, "Invalid metadata.", cancellationToken);
            return;
        }

        try
        {
            if (blobProperties.ContentLength > _blobStorageOptions.MaxScreenshotSize)
            {
                _logger.LogError("Screenshot '{BlobName}' exceeds size limit of {MaxSize} bytes.", blobName, _blobStorageOptions.MaxScreenshotSize);
                await DeleteInvalidBlobAsync(blobName, $"Screenshot size exceeds limit.", cancellationToken);
                return;
            }

            using var blobStream = await _blobStorageService.OpenReadAsync(_blobStorageOptions.QuarantineContainerName, blobName, cancellationToken);
            using var image = await Image.LoadAsync(blobStream, cancellationToken);

            if (image.Metadata.DecodedImageFormat is not (PngFormat or JpegFormat))
            {
                _logger.LogError("Screenshot '{BlobName}' is not a valid image format.", blobName);
                await DeleteInvalidBlobAsync(blobName, "Invalid image format.", cancellationToken);
                return;
            }

            using var thumbnailStream = new MemoryStream();
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(200, 200),
                Mode = ResizeMode.Max
            }));

            await image.SaveAsync(thumbnailStream, image.Metadata.DecodedImageFormat, cancellationToken);
            thumbnailStream.Position = 0;
            blobStream.Position = 0;

            using var sha = SHA256.Create();
            var computedChecksum = BitConverter.ToString(await sha.ComputeHashAsync(blobStream)).Replace("-", "").ToLowerInvariant();

            if (!metadata.TryGetValue("Checksum", out var expectedChecksum) || !string.Equals(computedChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Checksum mismatch for screenshot '{BlobName}'.", blobName);
                await DeleteInvalidBlobAsync(blobName, $"Checksum mismatch.", cancellationToken);
                return;
            }

            var tags = new Dictionary<string, string>
            {
                { "scan-status", "clean" },
                { "appId", appId.ToString() },
                { "checksum", computedChecksum }
            };
            await _blobStorageService.SetBlobTagsAsync(_blobStorageOptions.QuarantineContainerName, blobName, tags, cancellationToken);

            var processedEvent = new ScreenshotProcessedEvent
            {
                BlobName = blobName,
                AppId = appId,
                ScreenshotId = screenshotId,
                Checksum = computedChecksum
            };

            await _eventPublisher.PublishAsync(KafkaTopics.ScreenshotResultEvents, processedEvent, cancellationToken);
            _logger.LogInformation("Screenshot '{BlobName}' processed successfully at {ContainerName}.", blobName, _blobStorageOptions.QuarantineContainerName);

        }
        catch (System.Exception ex)
        {
            _logger.LogError("Error processing screenshot '{BlobName}' in container '{ContainerName}': Message: {Message}", blobName, _blobStorageOptions.QuarantineContainerName, ex.Message);
            await DeleteInvalidBlobAsync(blobName, $"Processing error: {ex.Message}", cancellationToken);
            throw;
        }

    }

    private async Task DeleteInvalidBlobAsync(string blobName, string reason, CancellationToken cancellationToken)
    {
        await _blobStorageService.DeleteAsync(_blobStorageOptions.QuarantineContainerName, blobName, cancellationToken);
        _logger.LogWarning("Security alert: Invalid screenshot '{BlobName}' deleted from quarantine. Reason: {Reason}", blobName, reason);
    }
}