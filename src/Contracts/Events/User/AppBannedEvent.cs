using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events.User
{
    public record AppBannedEvent
    {
        [Required]
        public Guid ActionId { get; init; }
        [Required]
        public Guid AppId { get; init; }
        [Required]
        public Guid AdminId { get; init; }
        [Required]
        public string Reason { get; init; } = null!;
        [Required]
        public DateTime ActionDate { get; init; }
    }
}
