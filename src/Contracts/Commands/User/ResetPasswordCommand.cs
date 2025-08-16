using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.User
{
    public record ResetPasswordCommand :IRequest
    {

        [Required]
        public string ResetToken { get; init; } = null!;
        [Required]
        public string NewPassword { get; init; } = null!;
    }
}
