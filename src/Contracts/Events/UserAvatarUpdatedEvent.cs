using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events
{
    public record UserAvatarUpdatedEvent
    {
        public Guid UserId { get; init; }
        [Required]
        public string Email { get; init; } = null!;
        [MaxLength(500)]
        public string? AvatarUrl { get; init; }
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime UpdatedAt { get; init; }
    }
}
