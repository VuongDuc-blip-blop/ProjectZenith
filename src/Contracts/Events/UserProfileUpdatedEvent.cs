using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events
{
    public record UserProfileUpdatedEvent
    {

        public Guid UserId { get; init; }
        [Required]
        public string Email { get; init; } = null!;
        [Required]
        public string UserName { get; init; } = null!;
        [MaxLength(500)]
        public string? Bio { get; init; }
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime UpdatedAt { get; init; }
    }
}
