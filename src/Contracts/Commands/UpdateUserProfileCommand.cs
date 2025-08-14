using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands
{
    public record UpdateUserProfileCommand
    {

        public Guid UserId { get; init; }
        [Required]
        [MaxLength(100)]
        public string DisplayName { get; init; } = null!;
        [MaxLength(500)]
        public string? Bio { get; init; }
    }
}
