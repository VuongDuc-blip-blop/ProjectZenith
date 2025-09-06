using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events.User
{
    /// <summary>
    /// Event triggered when a user's password reset is completed.
    /// </summary>
    public record PasswordResetCompletedEvent
    {
        /// <summary>
        /// The unique identifier for the user.
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// The email address of the user.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; init; }

        /// <summary>
        /// The date and time when the password reset occurred.
        /// </summary>
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime ResetAt { get; init; }
    }
}
