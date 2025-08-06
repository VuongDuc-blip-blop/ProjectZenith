using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands
{
    public record VerifyEmailCommand
    {
        [Required(ErrorMessage = "Token is required")]
        public string Token { get; init; } = null!;
    }
}
