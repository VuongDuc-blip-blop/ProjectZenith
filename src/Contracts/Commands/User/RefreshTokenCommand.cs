using MediatR;
using ProjectZenith.Contracts.DTOs.User;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.User
{
    public record RefreshTokenCommand : IRequest<RefreshTokenResponseDTO>
    {
        [Required(ErrorMessage = "Refresh token ID is required.")]
        public Guid RefreshTokenId { get; init; }
        [Required(ErrorMessage = "Refresh token is required.")]
        public string RefreshToken { get; init; } = null!;
    }
}
