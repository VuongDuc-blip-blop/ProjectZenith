using ProjectZenith.Contracts.Enums;


namespace ProjectZenith.Contracts.Events.Developer
{
    public record PayoutOnboardingCompletedEvent
    {
        public Guid DeveloperId { get; init; }
        public string StripeAccountId { get; init; } = string.Empty;
        public DeveloperPayoutStatus PayoutStatus { get; init; }
        public DateTime CompletedAt { get; init; }
    }
}
