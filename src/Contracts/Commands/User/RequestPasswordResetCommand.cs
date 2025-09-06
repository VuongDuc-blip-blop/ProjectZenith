using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.User
{
    /// <summary>
    /// Command to request a password reset.
    /// </summary>
    public record RequestPasswordResetCommand : IRequest
    {
        /// <summary>
        /// The email of the user requesting a password reset.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; init; } = null!;
    }
}
