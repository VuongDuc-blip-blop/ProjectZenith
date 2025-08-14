using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events
{
    public record UserLoggedOutEvent
    {
        [Required]
        public Guid UserId { get; init; }

        [Required]
        public string Email { get; init; } = null!;

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime LoggedOutAt { get; init; }
    }
}
