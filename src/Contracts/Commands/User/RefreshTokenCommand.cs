using MediatR;
using ProjectZenith.Contracts.DTOs.User;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.User
{
    /// <summary>
    /// Command to refresh a user's authentication token.
    /// </summary>
    public record RefreshTokenCommand : IRequest<RefreshTokenResponseDTO>
    {
        /// <summary>
        /// The unique identifier of the refresh token.
        /// </summary>
        [Required(ErrorMessage = "Refresh token ID is required.")]
        public Guid RefreshTokenId { get; init; }

        /// <summary>
        /// The refresh token used to obtain a new access token.
        /// </summary>
        [Required(ErrorMessage = "Refresh token is required.")]
        public string RefreshToken { get; init; } = null!;
    }
}
