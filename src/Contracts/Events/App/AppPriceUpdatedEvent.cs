namespace ProjectZenith.Contracts.Events.App
{
    public record AppPriceUpdatedEvent
    {
        public Guid AppId { get; init; }
        public Guid DeveloperId { get; init; }
        public decimal NewPrice { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
