using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events.User
{
    public record UserEmailVerifiedEvent
    {

        public Guid UserId { get; init; }

        [Required]
        public string Email { get; init; } = null!;

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime VerifiedAt { get; init; }
    }
}
