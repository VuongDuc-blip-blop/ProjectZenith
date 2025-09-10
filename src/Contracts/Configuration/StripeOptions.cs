namespace ProjectZenith.Contracts.Configuration
{
    public class StripeOptions
    {
        public const string SectionName = "Stripe";
        public string PublishableKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string ConnectAccountId { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
    }
}
