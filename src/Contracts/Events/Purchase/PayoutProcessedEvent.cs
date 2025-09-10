namespace ProjectZenith.Contracts.Events.Purchase
{
    public record PayoutProcessedEvent
    {
        public Guid PayoutId { get; init; }
        public Guid DeveloperId { get; init; }
        public decimal Amount { get; init; }
        public string PaymentId { get; init; } = string.Empty;
        public DateTime ProcessedAt { get; init; }
    }
}
