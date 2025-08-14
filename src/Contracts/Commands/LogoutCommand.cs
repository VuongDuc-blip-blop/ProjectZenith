using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands
{
    public record LogoutCommand
    {
        [Required]
        public string RefreshToken { get; init; } = null!;
    }
}
