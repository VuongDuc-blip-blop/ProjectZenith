using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events.User
{
    /// <summary>
    /// Event triggered when a user logs in.
    /// </summary>
    public class UserLoggedInEvent
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
        /// The date and time when the user logged in.
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime LoggedInAt { get; init; }
    }
}
