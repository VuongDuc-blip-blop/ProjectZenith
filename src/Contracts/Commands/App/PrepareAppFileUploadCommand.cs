using MediatR;
using ProjectZenith.Contracts.DTOs.App;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.App
{
    /// <summary>
    /// Command to prepare the application file upload.
    /// </summary>
    public record PrepareAppFileUploadCommand : IRequest<PrepareUploadResponseDTO>
    {
        /// <summary>
        /// The unique identifier of the developer preparing the application file upload.
        /// </summary>
        public Guid DeveloperId { get; init; }

        /// <summary>
        /// The name of the application being uploaded.
        /// </summary>
        [Required]
        public string AppName { get; init; } = null!;

        /// <summary>
        /// The version number of the application being uploaded.
        /// </summary>
        [Required]
        public string VersionNumber { get; init; } = null!;

        /// <summary>
        /// The name of the file being uploaded.
        /// </summary>
        [Required]
        public string FileName { get; init; } = null!;

        /// <summary>
        /// The size of the file being uploaded.
        /// </summary>
        [Required]
        [Range(0, 500 * 1024 * 1024, ErrorMessage = "File size cannot exceed 500 MB.")]
        public long FileSize { get; init; }

        /// <summary>
        /// The content type of the file being uploaded.
        /// </summary>
        [Required]
        public string ContentType { get; init; } = null!;

        [Required]
        public IReadOnlyList<string>? ScreenshotFileNames { get; init; }
    }
}
