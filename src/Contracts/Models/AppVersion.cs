using System.ComponentModel.DataAnnotations;
using ProjectZenith.Contracts.Enums;

namespace ProjectZenith.Contracts.Models
{

    /// <summary>
    /// Represents a version of an application.
    /// </summary>
    public class AppVersion
    {
        /// <summary>
        /// The unique identifier for the version.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// The unique identifier for the application this version belongs to.
        /// </summary>
        [Required]
        public Guid AppId { get; set; }

        /// <summary>
        /// The version number of the application.
        /// </summary>
        [Required]
        [StringLength(50, ErrorMessage = "Version number cannot be longer than 50 characters.")]
        public string VersionNumber { get; set; } = null!;

        /// <summary>
        /// the changelog for this version.
        /// </summary>
        [StringLength(2000, ErrorMessage = "Changelog cannot be longer than 2000 characters.")]
        public string? Changelog { get; set; } = string.Empty;

        /// <summary>
        /// The status of this version.
        /// </summary>
        public Status Status { get; set; } = Status.PendingValidation;

        /// <summary>
        /// The reason for the application's current status.
        /// </summary>
        [StringLength(500, ErrorMessage = "Status reason cannot be longer than 500 characters.")]
        public string? StatusReason { get; set; }

        /// <summary>
        /// The unique identifier for the field this version belongs to.
        /// </summary>
        [Required]
        public Guid FileId { get; set; }

        /// <summary>
        /// The date and time when this version was created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// the application this version belongs to.
        /// </summary>
        public App App { get; set; } = null!;

        /// <summary>
        /// The file associated with this version.
        /// </summary>
        public AppFile File { get; set; } = null!;
    }
}
