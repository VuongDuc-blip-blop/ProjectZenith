using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.User
{
    /// <summary>
    /// Command to update a user's profile.
    /// </summary>
    public record UpdateUserProfileCommand : IRequest
    {
        /// <summary>
        /// The unique identifier of the user whose profile is to be updated.
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// The new display name for the user.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string DisplayName { get; init; } = null!;

        /// <summary>
        /// The new bio for the user.
        /// </summary>
        [MaxLength(500)]
        public string? Bio { get; init; }
    }
}
