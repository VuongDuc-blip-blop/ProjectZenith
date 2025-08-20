using MediatR;
using ProjectZenith.Contracts.DTOs.User;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.User
{
    public record RequestDeveloperStatusCommand : IRequest<LoginResponseDTO>
    {
        public Guid UserId;


        [MaxLength(1000)]
        public string? Description;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string ContactEmail = null!;
    }
}
