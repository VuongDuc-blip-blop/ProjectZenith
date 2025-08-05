using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectZenith.Contracts.Models
{
    /// <summary>
    /// Represents a system log entry for tracking user actions.
    /// </summary>
    public class SystemLog
    {
        /// <summary>
        /// Unique identifier for the system log entry.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The unique identifier for the user associated with this log entry.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// The action performed by the user, such as "Login", "Purchase", etc.
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "Action cannot be longer than 100 characters.")]
        public string Action { get; set; } = null!;

        /// <summary>
        /// The details of the action performed, if applicable.
        /// </summary>
        [StringLength(1000, ErrorMessage = "Details cannot be longer than 1000 characters.")]
        public string? Details { get; set; }

        /// <summary>
        /// The IP address of the user when the action was performed.
        /// </summary>
        [StringLength(45, ErrorMessage = "IP address cannot be longer than 45 characters.")]
        public string? IpAddress { get; set; }

        /// <summary>
        /// The timestamp when the action was performed.
        /// This is set to the current UTC time when the log entry is created.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property to the user associated with this log entry.
        /// </summary>
        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
