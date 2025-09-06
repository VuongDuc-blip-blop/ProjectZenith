namespace ProjectZenith.Contracts.DTOs.App
{
    public record PrepareMediaUploadResponseDTO(IReadOnlyList<(string BlobName, string SasUrl)> UploadDetails);
}
