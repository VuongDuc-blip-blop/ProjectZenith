using MediatR;
using ProjectZenith.Contracts.Events.User;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.User
{
    /// <summary>
    /// Command to verify a user's email.
    /// </summary>
    public record VerifyEmailCommand : IRequest<UserEmailVerifiedEvent>
    {
        /// <summary>
        /// The token used to verify the user's email.
        /// </summary>
        [Required(ErrorMessage = "Token is required")]
        public string Token { get; init; } = null!;
    }
}
