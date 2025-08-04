using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectZenith.Contracts.Models
{
    /// <summary>
    /// Represents a file in the system.
    /// </summary>
    public class File
    {
        /// <summary>
        /// The unique identifier for the file.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(2083, ErrorMessage = "File path cannot be longer than 2083 characters.")]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// The size of the file in bytes.
        /// </summary>
        [Required]
        public long Size { get; set; }

        [Required]
        [StringLength(64, ErrorMessage = "Checksum cannot be longer than 64 characters.")]
        public string Checksum { get; set; } = string.Empty;

        /// <summary>
        /// The date and time when the file was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
