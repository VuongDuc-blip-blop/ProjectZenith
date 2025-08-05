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
    /// Represents the status of a moderation action.
    /// </summary>
    public enum ModerationActionStatus
    {
        Pending,
        Completed,
        Reversed
    }

    /// <summary>
    /// Represents target types for moderation actions.
    /// </summary>
    public enum ModerationActionTargetType
    {
        User,
        App,
        Review,
        AbuseReport
    }

    /// <summary>
    /// Represents a moderation action taken by an admin.
    /// </summary>
    public class ModerationAction
    {
        /// <summary>
        /// the unique identifier for the moderation action.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// The unique identifier for the admin who performed the action.
        /// </summary>
        public Guid AdminId { get; set; }

        /// <summary>
        /// The type of moderation action performed, such as banning a user or removing an app.
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "Action type cannot be longer than 100 characters.")]
        public string ActionType { get; set; } = null!;

        /// <summary>
        /// The reason for the moderation action, if applicable.
        /// </summary>
        [StringLength(500, ErrorMessage = "Reason cannot be longer than 500 characters.")]
        public string? Reason { get; set; }

        /// <summary>
        /// The status of the moderation action, indicating whether it is pending, completed, or reversed.
        /// </summary>
        public ModerationActionStatus Status { get; set; } = ModerationActionStatus.Completed;

        /// <summary>
        /// The date and time when the moderation action was performed.
        /// </summary>
        public DateTime ActionDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The type of target affected by the moderation action (e.g., User, App, Review).
        /// </summary>
        [Required]
        public ModerationActionTargetType TargetType { get; set; }

        /// <summary>
        /// The unique identifier of the target affected by the moderation action.
        /// </summary>
        public Guid TargetId { get; set; }

        /// <summary>
        /// Navigation property to the admin who performed the action.
        /// </summary>
        [ForeignKey("AdminId")]
        public User Admin { get; set; } = null!;
    }
}
