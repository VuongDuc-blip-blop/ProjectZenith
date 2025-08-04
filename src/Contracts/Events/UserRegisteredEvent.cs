using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events
{
    /// <summary>
    /// Published when a user sucessfully registers in the system.
    /// </summary>
    public class UserRegisteredEvent
    {
        /// <summary>
        /// Unique identifier for the user.
        /// </summary>
        [Required]
        public Guid UserId { get; init; }

        /// <summary>
        /// The email address of the user.
        /// </summary>
        [Required]
        public string Email { get; init; }

        /// <summary>
        /// The user's optional username.
        /// </summary>
        [Required]
        public string Username { get; init; }

        /// <summary>
        /// The UTC timestamp when the user registered.
        /// </summary>
        [Required]
        public DateTime RegisteredAt { get; init; }
    }
}
