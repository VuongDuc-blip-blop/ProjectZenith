using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands
{
    public record RequestPasswordResetCommand
    {
        [Required]
        [EmailAddress]
        public string Email { get; init; } = null!;
    }
}
