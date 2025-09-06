using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events.User
{
    /// <summary>
    /// Event triggered when a user's email is verified.
    /// </summary>
    public record UserEmailVerifiedEvent
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
        /// The date and time when the email was verified.
        /// </summary>
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime VerifiedAt { get; init; }
    }
}
