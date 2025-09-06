using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.User
{
    /// <summary>
    /// Command to reset a user's password.
    /// </summary>
    public record ResetPasswordCommand : IRequest
    {
        /// <summary>
        /// The reset token for the password reset request.
        /// </summary>
        [Required]
        public string ResetToken { get; init; } = null!;

        /// <summary>
        /// The new password for the user.
        /// </summary>
        [Required]
        public string NewPassword { get; init; } = null!;
    }
}
