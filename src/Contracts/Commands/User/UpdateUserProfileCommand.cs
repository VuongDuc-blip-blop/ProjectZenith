using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.User
{
    public record UpdateUserProfileCommand :IRequest
    {

        public Guid UserId { get; init; }
        [Required]
        [MaxLength(100)]
        public string DisplayName { get; init; } = null!;
        [MaxLength(500)]
        public string? Bio { get; init; }
    }
}
