using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands
{
    public record ResetPasswordCommand
    {

        [Required]
        public string ResetToken { get; init; } = null!;
        [Required]
        public string NewPassword { get; init; } = null!;
    }
}
