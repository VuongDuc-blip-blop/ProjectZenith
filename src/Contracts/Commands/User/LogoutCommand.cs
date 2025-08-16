using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.User
{
    public record LogoutCommand:IRequest
    {
        [Required]
        public string RefreshToken { get; init; } = null!;
    }
}
