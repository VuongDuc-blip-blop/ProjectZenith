using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Models
{
    /// <summary>
    /// Represents a file in the system.
    /// </summary>
    public class AppFile
    {
        /// <summary>
        /// The unique identifier for the file.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(1024, ErrorMessage = "File path cannot be longer than 1024 characters.")]
        public string Path { get; set; } = null!;

        /// <summary>
        /// The size of the file in bytes.
        /// </summary>
        [Required]
        public long Size { get; set; }

        [Required]
        [StringLength(64, ErrorMessage = "Checksum cannot be longer than 64 characters.")]
        public string Checksum { get; set; } = null!;

        /// <summary>
        /// The date and time when the file was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The AppVersion this file belong to
        /// </summary>
        public AppVersion Version { get; set; } = null!;
    }
}
