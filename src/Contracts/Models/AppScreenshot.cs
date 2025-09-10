using System.ComponentModel.DataAnnotations;
using ProjectZenith.Contracts.Enums;
using ProjectZenith.Contracts.Interfaces;

namespace ProjectZenith.Contracts.Models
{
    /// <summary>
    /// Represents a screenshot associated with an application.
    /// </summary>
    public class AppScreenshot : ISoftDeletable
    {
        /// <summary>
        /// The unique identifier for the screenshot.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// The unique identifier of the application this screenshot is associated with.
        /// </summary>
        [Required]
        public Guid AppId { get; set; }

        /// <summary>
        /// Path to the screenshot file in blob storage.
        /// </summary>
        [Required]
        [StringLength(1024, ErrorMessage = "Path cannot be longer than 1024 characters.")]
        public string Path { get; set; } = null!;

        /// <summary>
        /// The current status of the screenshot.
        /// </summary>
        [Required]
        public ScreenshotStatus Status { get; set; }

        /// <summary>
        /// Size of the screenshot file in bytes.
        /// </summary>
        [Required]
        public long Size { get; set; }

        /// <summary>
        /// Checksum of the screenshot file for integrity verification.
        /// </summary>
        [Required]
        [StringLength(64, ErrorMessage = "Checksum cannot be longer than 64 characters.")]
        public string Checksum { get; set; } = null!;

        /// <summary>
        /// Timestamp when the screenshot was uploaded.
        /// </summary>
        [Required]
        public DateTime UploadedAt { get; set; }

        /// <summary>
        /// Indicates whether the screenshot has been soft deleted.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Timestamp when the screenshot was soft deleted.
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        // Navigation property
        public App App { get; set; }
    }
}
