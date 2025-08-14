using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands
{
    public record RefreshTokenCommand
    {
        [Required(ErrorMessage = "Refresh token ID is required.")]
        public Guid RefreshTokenId { get; init; }
        [Required(ErrorMessage = "Refresh token is required.")]
        public string RefreshToken { get; init; } = null!;
    }
}
