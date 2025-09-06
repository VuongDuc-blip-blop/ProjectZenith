namespace ProjectZenith.Contracts.DTOs.App
{
    /// <summary>
    /// DTO for preparing a file upload.
    /// </summary>
    public record PrepareUploadResponseDTO
    {
        /// <summary>
        /// The unique identifier for the submission.
        /// </summary>
        public Guid SubmissionId { get; init; }

        /// <summary>
        /// The secured URL for uploading the file.
        /// </summary>
        public string SecuredUploadUrl { get; init; } = string.Empty;

        /// <summary>
        /// The name of the blob to be created.
        /// </summary>
        public string UploadSasToken { get; init; } = string.Empty;
    }
}
