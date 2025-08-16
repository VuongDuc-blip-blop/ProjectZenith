using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events.User
{
    public record PasswordResetCompletedEvent
    {
        public Guid UserId { get; init; }
        [Required]
        [EmailAddress]
        public string Email { get; init; }
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime ResetAt { get; init; }
    }
}
