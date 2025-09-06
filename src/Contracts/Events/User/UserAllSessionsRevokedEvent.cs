using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events.User
{
    /// <summary>
    /// Event triggered when a user's all sessions are revoked.
    /// </summary>
    public record UserAllSessionsRevokedEvent
    {
        /// <summary>
        /// The unique identifier for the user.
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// The email address of the user.
        /// </summary>
        [Required]
        public string Email { get; init; }

        /// <summary>
        /// The date and time when the sessions were revoked.
        /// </summary>
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime RevokedAt { get; init; }
    }
}
