using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events.User
{
    /// <summary>
    /// Event triggered when a user's password reset is requested.
    /// </summary>
    public record PasswordResetRequestedEvent
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
        /// The date and time when the password reset was requested.
        /// </summary>
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime RequestedAt { get; init; }
    }
}
