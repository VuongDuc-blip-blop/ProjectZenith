using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.DTOs
{
    /// <summary>
    /// Represents a user's profile information.
    /// This DTO is used to transfer user profile data between layers of the application.
    /// </summary>
    /// <param name="UserId">The unique identifier of the user.</param>
    /// <param name="Email">The email address of the user.</param>
    /// <param name="Username">The optional username chosen by the user.</param>
    /// <param name="Bio">An optional biography or description of the user.</param>
    /// <param name="AvatarUrl">An optional URL to the user's avatar image.</param>
    public record UserProfileDto
    {
        [Required]
        public Guid UserId { get; init; }
        [Required]
        public string Email { get; init; }
        public string? Username { get; init; }
        public string? Bio { get; init; }
        public string? AvatarUrl { get; init; }
    }
}
