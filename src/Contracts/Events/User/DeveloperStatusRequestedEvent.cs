using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events.User
{
    /// <summary>
    /// Event triggered when a user's developer status is requested.
    /// </summary>
    public record DeveloperStatusRequestedEvent
    {
        /// <summary>
        /// The unique identifier for the user.
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// The description of the request.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// The contact email for the user.
        /// </summary>
        [Required]
        [EmailAddress]
        public string ContactEmail { get; init; } = null!;
    }
}
