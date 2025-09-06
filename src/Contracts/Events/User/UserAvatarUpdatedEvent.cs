using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events.User
{
    /// <summary>
    /// Event triggered when a user's avatar is updated.
    /// </summary>
    public record UserAvatarUpdatedEvent
    {
        /// <summary>
        /// The unique identifier for the user.
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// The email address of the user.
        /// </summary>
        [Required]
        public string Email { get; init; } = null!;

        /// <summary>
        /// The URL of the user's avatar.
        /// </summary>
        [MaxLength(500)]
        public string? AvatarUrl { get; init; }

        /// <summary>
        /// The date and time when the avatar was updated.
        /// </summary>
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime UpdatedAt { get; init; }
    }
}
