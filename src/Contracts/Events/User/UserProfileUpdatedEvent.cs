using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events.User
{
    public record UserProfileUpdatedEvent
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
        /// The user name of the user.
        /// </summary>
        [Required]
        public string UserName { get; init; } = null!;

        /// <summary>
        /// The bio of the user.
        /// </summary>
        [MaxLength(500)]
        public string? Bio { get; init; }

        /// <summary>
        /// The date and time when the user profile was updated.
        /// </summary>
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime UpdatedAt { get; init; }
    }
}
