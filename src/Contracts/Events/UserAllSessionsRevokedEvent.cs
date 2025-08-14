using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events
{
    public record UserAllSessionsRevokedEvent
    {
        public Guid UserId { get; init; }
        [Required]
        public string Email { get; init; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime RevokedAt { get; init; }
    }
}
