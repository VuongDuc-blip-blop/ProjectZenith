using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.User
{
    public record RequestPasswordResetCommand : IRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; init; } = null!;
    }
}
