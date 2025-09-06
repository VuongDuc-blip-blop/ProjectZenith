using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;

namespace ProjectZenith.Contracts.Infrastructure
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<BlobStorageService> _logger;

        public BlobStorageService(string storageAccountName, ILogger<BlobStorageService> logger)
        {
            _logger = logger;
            var credential = new EnvironmentCredential();
            var blobUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");
            _blobServiceClient = new BlobServiceClient(blobUri, credential);
        }

        public async Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken cancellationToken)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            return await blobClient.ExistsAsync(cancellationToken);
        }

        public async Task<Stream> OpenReadAsync(string containerName, string blobName, CancellationToken cancellationToken)
        {
            var container = _blobServiceClient.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);

            if (!await blob.ExistsAsync(cancellationToken))
            {
                throw new FileNotFoundException($"Blob '{blobName}' not found in container '{containerName}'.");
            }

            // Open stream for reading
            BlobDownloadInfo download = await blob.DownloadAsync(cancellationToken);
            return download.Content;
        }

        public async Task<string> GetUserDelegationSasAsync(string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException("Container name cannot be null or empty.", nameof(containerName));

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            if (!await containerClient.ExistsAsync())
                throw new InvalidOperationException($"Container '{containerName}' does not exist.");

            // Get user delegation key valid for 1 hour
            var delegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(
                DateTimeOffset.UtcNow.AddMinutes(-5), // start before 5 minutes to account for clock skew
                DateTimeOffset.UtcNow.AddHours(1));

            // Build SAS token
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                Resource = "c", // "c" means container
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Add | BlobSasPermissions.Write | BlobSasPermissions.Create);


            // Create SAS query parameters using the delegation key
            var sasToken = sasBuilder.ToSasQueryParameters(delegationKey.Value, _blobServiceClient.AccountName).ToString();

            // Return the container URI with the SAS token
            return $"{containerClient.Uri}?{sasToken}";
        }

        public async Task<BlobProperties?> GetPropertiesAsync(string containerName, string blobName, CancellationToken cancellationToken)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                return null;
            }

            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            return properties.Value;
        }

        public async Task MoveAsync(string sourceContainerName, string sourceBlobName, string destinationContainerName, string destinationBlobName, CancellationToken cancellationToken)
        {
            var sourceContainerClient = _blobServiceClient.GetBlobContainerClient(sourceContainerName);
            var sourceBlobClient = sourceContainerClient.GetBlobClient(sourceBlobName);

            if (!await sourceBlobClient.ExistsAsync(cancellationToken))
            {
                throw new FileNotFoundException($"Source blob '{sourceBlobName}' not found in container '{sourceContainerName}'.");
            }

            var destinationContainerClient = _blobServiceClient.GetBlobContainerClient(destinationContainerName);
            var destinationBlobClient = destinationContainerClient.GetBlobClient(destinationBlobName);

            // Start the copy operation
            var copyOperation = await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri, cancellationToken: cancellationToken);

            // Optionally, wait for the copy to complete
            BlobProperties properties;
            do
            {
                await Task.Delay(500, cancellationToken); // Wait before checking the status again
                properties = await destinationBlobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            } while (properties.CopyStatus == CopyStatus.Pending);

            if (properties.CopyStatus != CopyStatus.Success)
            {
                throw new InvalidOperationException($"Failed to copy blob. Status: {properties.CopyStatus}, Description: {properties.CopyStatusDescription}");
            }

            // Delete the source blob after successful copy
            await sourceBlobClient.DeleteAsync(cancellationToken: cancellationToken);
        }

        public async Task SetBlobMetadataAsync(string containerName, string blobName, IDictionary<string, string> metadata, CancellationToken cancellationToken)
        {
            // Get a client for the specific blob
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Check if the blob exists before trying to set metadata
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                throw new FileNotFoundException($"Blob '{blobName}' not found in container '{containerName}'. Cannot set metadata.");
            }

            try
            {
                // Call the SDK method to set the metadata
                await blobClient.SetMetadataAsync(metadata, cancellationToken: cancellationToken);
            }
            catch (RequestFailedException ex)
            {
                // Provide more context if the operation fails
                _logger.LogError(ex, "Failed to set metadata for blob '{BlobName}' in container '{ContainerName}'.", blobName, containerName);
                throw; // Re-throw the exception
            }
        }


        public async Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken)
        {
            try
            {
                // Get a client for the specific blob
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                // Call the SDK method to delete the blob.
                // DeleteIfExistsAsync is convenient because it won't throw an exception
                // if the blob is already gone, which is a safe behavior in distributed systems.
                // It returns true if the blob was deleted, and false if it did not exist.
                bool deleted = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);

                if (deleted)
                {
                    _logger.LogInformation("Successfully deleted blob '{BlobName}' from container '{ContainerName}'.", blobName, containerName);
                }
                else
                {
                    _logger.LogWarning("Attempted to delete blob '{BlobName}' from container '{ContainerName}', but it did not exist.", blobName, containerName);
                }
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Failed to delete blob '{BlobName}' from container '{ContainerName}'.", blobName, containerName);
                throw;
            }
        }

        public async Task SetBlobTagsAsync(string containerName, string blobName, IDictionary<string, string> tags, CancellationToken cancellationToken)
        {
            try
            {
                // Get a client for the specific blob
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                // Check if the blob exists before trying to set tags
                if (!await blobClient.ExistsAsync(cancellationToken))
                {
                    throw new FileNotFoundException($"Blob '{blobName}' not found in container '{containerName}'. Cannot set tags.");
                }

                // Call the SDK method to set the index tags
                await blobClient.SetTagsAsync(tags, cancellationToken: cancellationToken);

                _logger.LogInformation("Successfully set tags for blob '{BlobName}' in container '{ContainerName}'.", blobName, containerName);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Failed to set tags for blob '{BlobName}' in container '{ContainerName}'. Error: {ErrorMessage}", blobName, containerName, ex.Message);
                // Có thể lỗi xảy ra do tên hoặc giá trị của tag không hợp lệ.
                // Azure có các quy tắc về ký tự được phép trong key và value của tag.
                throw;
            }
        }
    }
}
