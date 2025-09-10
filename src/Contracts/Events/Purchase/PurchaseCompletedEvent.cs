namespace ProjectZenith.Contracts.Events.Purchase
{
    public record PurchaseCompletedEvent
    {
        public Guid PurchaseId { get; init; }
        public Guid UserId { get; init; }
        public Guid AppId { get; init; }
        public Guid DeveloperId { get; init; }
        public decimal Price { get; init; }
        public string PaymentId { get; init; } = string.Empty;
        public DateTime CompletedAt { get; init; }
    }
}
