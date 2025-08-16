using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events.User
{
    public record DeveloperStatusRequestedEvent
    {
        public Guid UserId { get; init; }
        public string? Description { get; init; }
        [Required]
        [EmailAddress]
        public string ContactEmail { get; init; } = null!;
    }
}
