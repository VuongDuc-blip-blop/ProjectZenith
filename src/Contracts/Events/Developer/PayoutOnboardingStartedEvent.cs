namespace ProjectZenith.Contracts.Events.Developer
{
    public record PayoutOnboardingStartedEvent(Guid DeveloperId, string StripeAccountId, DateTime StartedAt);
}
