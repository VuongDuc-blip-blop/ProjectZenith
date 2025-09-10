// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using FileScan.Functions.Services.VirusTotal;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Contracts.Events.App;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Infrastructure.Messaging;
using ProjectZenith.Contracts.Validation;
using System.Security.Cryptography;
using static FileScan.Functions.Services.VirusTotal.IVirusScanService;

namespace FileScan.Functions
{
    public class ProcessUploadedApp
    {
        private readonly ILogger<ProcessUploadedApp> _logger;
        private readonly IBlobStorageService _blobStorageService;
        private readonly IFileSignatureValidator _fileSignatureValidator;
        private readonly BlobStorageOptions _blobStorageOptions;
        private readonly IVirusScanService _virusScanService;
        private readonly IEventPublisher _eventPublisher;
        private readonly KafkaOptions _kafkaOptions;



        private const long MAX_ALLOWED_FILE_SIZE = 500 * 1024 * 1024; // 500 MB

        public ProcessUploadedApp(
            ILogger<ProcessUploadedApp> logger,
            IBlobStorageService blobStorageService,
            IFileSignatureValidator fileSignatureValidator,
            IOptions<BlobStorageOptions> blobStorageOptions,
            IVirusScanService virusScanService,
            IEventPublisher eventPublisher,
            IOptions<KafkaOptions> kafkaOptions)
        {
            _logger = logger;
            _blobStorageService = blobStorageService;
            _fileSignatureValidator = fileSignatureValidator;
            _blobStorageOptions = blobStorageOptions.Value;
            _virusScanService = virusScanService;
            _eventPublisher = eventPublisher;
            _kafkaOptions = kafkaOptions.Value;
        }
        [Function("ProcessUploadedApp")]
        public async Task Run(
        [QueueTrigger("app-files-pending-validation", Connection = "AzureWebJobsStorage")] string queueMessage, CancellationToken cancellationToken)
        {


            // Get blob name from the queue message
            string blobName = queueMessage;

            _logger.LogInformation("Processing app file '{blobName}' from container '{containerName}'.", blobName, _blobStorageOptions.QuarantineContainerName);


            try
            {
                var blobProperties = await _blobStorageService.GetPropertiesAsync(_blobStorageOptions.QuarantineContainerName, blobName, default);
                if (blobProperties == null || blobProperties.ContentLength > MAX_ALLOWED_FILE_SIZE)
                {
                    _logger.LogError("Blob properties not found for '{blobName}'.", blobName);
                    await PublishValidationFailedEventAsync(blobName, Guid.Empty, Guid.Empty, "File not found or exceeds size limit.", $"oversize/{blobName}", default);
                    return;
                }

                var metadata = blobProperties.Metadata;
                if (!metadata.TryGetValue("AppId", out var appIdStr) || !Guid.TryParse(appIdStr, out var appId)
                || !metadata.TryGetValue("AppFileId", out var appFileIdStr) || !Guid.TryParse(appFileIdStr, out var appFileId))
                {
                    _logger.LogError("Metadata 'AppId' or 'AppFileId' missing or invalid for blob '{blobName}'.", blobName);
                    await PublishValidationFailedEventAsync(blobName, Guid.Empty, Guid.Empty, "Missing or invalid metadata.", $"invalid-metadata/{blobName}", default);
                    return;
                }



                _logger.LogInformation("Performing checksum validation for '{blobName}'.", blobName);
                string computedChecksum;
                using (var checksumStream = await _blobStorageService.OpenReadAsync(_blobStorageOptions.QuarantineContainerName, blobName, cancellationToken))
                {
                    using var sha = SHA256.Create();
                    var computedHash = await sha.ComputeHashAsync(checksumStream);
                    computedChecksum = BitConverter.ToString(computedHash).Replace("-", "").ToLowerInvariant();
                }

                if (!metadata.TryGetValue("Checksum", out var expectedChecksum) || !string.Equals(computedChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase))
                {
                    await PublishValidationFailedEventAsync(blobName, appId, appFileId, "Checksum mismatch.", $"checksum-mismatch/{blobName}", cancellationToken);
                    return;
                }

                _logger.LogInformation("Checksum validation passed for '{blobName}'.", blobName);


                // Validation 3: File Signature
                using (var sigStream = await _blobStorageService.OpenReadAsync(_blobStorageOptions.QuarantineContainerName, blobName, default))
                {
                    if (!await _fileSignatureValidator.IsValidFileSignature(sigStream, blobName))
                    {
                        await PublishValidationFailedEventAsync(blobName, appId, appFileId, "Invalid file signature.", $"invalid-signature/{blobName}", default);
                        return;
                    }
                }

                _logger.LogInformation("File signature validation passed for '{blobName}'.", blobName);

                // Validation 4: Virus Scan

                using (var scanStream = await _blobStorageService.OpenReadAsync(_blobStorageOptions.QuarantineContainerName, blobName, default))
                {
                    var scanResult = await _virusScanService.ScanFileAsync(scanStream, blobName, blobProperties.ContentLength, default);

                    if (scanResult.Status != ScanResultStatus.Safe)
                    {
                        string reason = scanResult.Status == ScanResultStatus.Malicious ? "Malware Detected" : "Virus scan failed or timeout";
                        string details = $"{reason}:{scanResult.Details}";
                        string prefix = scanResult.Status == ScanResultStatus.Malicious ? "malicious/" : "scan-failed/";
                        await PublishValidationFailedEventAsync(blobName, appId, appFileId, details, $"{prefix}{blobName}", default);
                        return;
                    }
                }


                _logger.LogInformation("All validations passed for '{blobName}'. Moving to validated container for admin approval.", blobName);
                var finalBlobName = $"{metadata["DeveloperId"]}/{SanitizeForUrl(metadata["AppName"])}/{metadata["VersionNumber"]}/{Path.GetFileName(blobName)}";
                await _blobStorageService.MoveAsync(
                    _blobStorageOptions.QuarantineContainerName,
                    blobName,
                    _blobStorageOptions.ValidatedContainerName,
                    finalBlobName,
                    cancellationToken);

                var @event = new AppFileValidatedEvent
                {
                    BlobName = blobName,
                    FinalPath = finalBlobName,
                    AppId = appId,
                    AppFileId = appFileId,
                    Checksum = computedChecksum
                };
                var appIdKey = @event.AppId.ToString();
                await _eventPublisher.PublishAsync(KafkaTopics.Apps, appIdKey, @event, cancellationToken);

                _logger.LogInformation("App file '{BlobName}' validated and moved to '{FinalPath}'.", blobName, finalBlobName);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing app file '{BlobName}': {Message}", blobName, ex.Message);
                await PublishValidationFailedEventAsync(blobName, Guid.Empty, Guid.Empty, $"Unexpected error: {ex.Message}", $"error/{blobName}", cancellationToken);
                throw;
            }

        }

        // --- CHANGE ---: Helper method to reduce code duplication for failure cases.
        private async Task PublishValidationFailedEventAsync(string blobName, Guid appId, Guid appFileId, string reason, string rejectedBlobPath, CancellationToken cancellationToken)
        {
            var @event = new AppFileValidationFailedEvent
            {
                BlobName = blobName,
                AppId = appId,
                AppFileId = appFileId,
                Reason = reason,
                RejectedPath = rejectedBlobPath
            };
            var appIdKey = @event.AppId.ToString();
            await _eventPublisher.PublishAsync(KafkaTopics.Apps, appIdKey, @event, cancellationToken);

            await _blobStorageService.MoveAsync(_blobStorageOptions.QuarantineContainerName, blobName, _blobStorageOptions.RejectedContainerName, rejectedBlobPath, cancellationToken);
            _logger.LogWarning("Validation failed for app file '{BlobName}'. Reason: {Reason}", blobName, reason);
        }

        private string SanitizeForUrl(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input.ToLowerInvariant(), @"[^a-z0-9\-]", "-").Trim('-');
        }
    }
}
