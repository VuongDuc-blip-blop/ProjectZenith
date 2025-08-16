using MediatR;
using ProjectZenith.Contracts.Events.User;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.User
{
    public record VerifyEmailCommand : IRequest<UserEmailVerifiedEvent>
    {
        [Required(ErrorMessage = "Token is required")]
        public string Token { get; init; } = null!;
    }
}
