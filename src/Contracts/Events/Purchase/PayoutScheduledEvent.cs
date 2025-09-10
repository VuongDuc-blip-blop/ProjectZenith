namespace ProjectZenith.Contracts.Events.Purchase
{
    public record PayoutScheduledEvent
    {
        public Guid PayoutId { get; init; }
        public Guid DeveloperId { get; init; }
        public decimal Amount { get; init; }
        public DateTime ScheduledAt { get; init; }
        public DateTime ProcessAt { get; init; }
    }
}
