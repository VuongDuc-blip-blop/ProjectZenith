using MediatR;
using ProjectZenith.Contracts.DTOs.User;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.User
{
    /// <summary>
    /// Command to request developer status for a user.
    /// </summary>
    public record RequestDeveloperStatusCommand : IRequest<LoginResponseDTO>
    {
        /// <summary>
        /// The unique identifier of the user requesting developer status.
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// The reason for requesting developer status.
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; init; }

        /// <summary>
        /// The contact email of the user requesting developer status.
        /// </summary>
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string ContactEmail { get; init; } = null!;
    }
}
