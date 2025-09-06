using Azure.Storage.Blobs.Models;

namespace ProjectZenith.Contracts.Infrastructure
{
    public interface IBlobStorageService
    {
        Task<string> GetUserDelegationSasAsync(string containerName);
        Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken cancellationToken);
        Task<Stream> OpenReadAsync(string containerName, string blobName, CancellationToken cancellationToken);
        Task<BlobProperties?> GetPropertiesAsync(string containerName, string blobName, CancellationToken cancellationToken);
        Task MoveAsync(string sourceContainerName, string sourceBlobName, string destinationContainerName, string destinationBlobName, CancellationToken cancellationToken);

        Task SetBlobMetadataAsync(string containerName, string blobName, IDictionary<string, string> metadata, CancellationToken cancellationToken);
        Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken);
        Task SetBlobTagsAsync(string containerName, string blobName, IDictionary<string, string> tags, CancellationToken cancellationToken);
    }
}
