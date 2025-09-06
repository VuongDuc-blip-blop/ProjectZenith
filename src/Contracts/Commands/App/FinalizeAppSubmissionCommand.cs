using System.ComponentModel.DataAnnotations;
using MediatR;
using ProjectZenith.Contracts.Enums;
using ProjectZenith.Contracts.Events.User;
using ProjectZenith.Contracts.Infrastructure;

namespace ProjectZenith.Contracts.Commands.App
{
    /// <summary>
    /// Command to finalize the submission of an application.
    /// </summary>
    public record FinalizeAppSubmissionCommand : IRequest<Guid>
    {
        /// <summary>
        /// The unique identifier of the developer submitting the application.
        /// </summary>
        public Guid DeveloperId { get; init; }

        /// <summary>
        /// The unique identifier of the submission being finalized.
        /// </summary>
        public Guid SubmissionId { get; init; }

        /// <summary>
        /// The name of the application being submitted.
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "Application's name cannot be longer than 100 characters.")]
        public string AppName { get; init; } = null!;
        /// <summary>
        /// The description of the application being submitted.
        /// </summary>
        [Required]
        [StringLength(1000, ErrorMessage = "Application's description cannot be longer than 1000 characters.")]
        public string Description { get; init; } = null!;

        /// <summary>
        /// The category of the application being submitted.
        /// </summary>
        [Required]
        [StringLength(50, ErrorMessage = "Application's category cannot be longer than 50 characters.")]
        public string Category { get; init; } = null!;

        /// <summary>
        /// The platform on which the application is being submitted.
        /// </summary>
        [Required]
        public Platform Platform { get; init; }

        /// <summary>
        /// The price of the application being submitted.
        /// </summary>
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a non-negative value.")]
        public decimal Price { get; init; }

        /// <summary>
        /// The version number of the application being submitted.
        /// </summary>
        [Required]
        [StringLength(50, ErrorMessage = "Version number cannot be longer than 50 characters.")]
        public string VersionNumber { get; init; } = null!;

        /// <summary>
        /// The changelog of the application being submitted.
        /// </summary>
        [StringLength(2000, ErrorMessage = "Changelog cannot be longer than 2000 characters.")]
        public string? Changelog { get; init; } = string.Empty;

        /// <summary>
        /// The name of the blob file where the application package is stored in blob storage.
        /// </summary>
        [Required]
        public string MainAppFileName { get; init; } = null!;

        /// <summary>
        /// The checksum of the application package file.
        /// </summary>
        [Required]
        public string MainAppChecksum { get; init; } = null!;

        /// <summary>
        /// The size of the application package file.
        /// </summary>
        [Required]
        public long MainAppFileSize { get; init; }

        /// <summary>
        /// The list of screenshots associated with the application submission.
        /// </summary>
        public IReadOnlyList<ScreenshotInfo> Screenshots { get; init; } = Array.Empty<ScreenshotInfo>();

        /// <summary>
        /// The list of tags associated with the application submission.
        /// </summary>
        public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    }

    public record ScreenshotInfo(string FileName, string Checksum, long Size);
}
