using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events.User
{
    /// <summary>
    /// Event triggered when a user's session is refreshed.
    /// </summary>
    public record UserSessionRefreshedEvent
    {
        /// <summary>
        /// The unique identifier for the user.
        /// </summary>
        [Required]
        public Guid UserId { get; init; }

        /// <summary>
        /// The email address of the user.
        /// </summary>
        [Required]
        public string Email { get; init; } = null!;

        /// <summary>
        /// The date and time when the user session was refreshed.
        /// </summary>
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime RefreshedAt { get; init; }
    }
}
