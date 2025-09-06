using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.User
{
    /// <summary>
    /// Command to log out a user.
    /// </summary>
    public record LogoutCommand : IRequest
    {
        /// <summary>
        /// The refresh token of the user logging out.
        /// </summary>
        [Required]
        public string RefreshToken { get; init; } = null!;
    }
}
