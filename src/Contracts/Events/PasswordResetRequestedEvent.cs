using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events
{
    public record PasswordResetRequestedEvent
    {
        public Guid UserId { get; init; }
        [Required]
        public string Email { get; init; } = null!;
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime RequestedAt { get; init; }
    }
}
