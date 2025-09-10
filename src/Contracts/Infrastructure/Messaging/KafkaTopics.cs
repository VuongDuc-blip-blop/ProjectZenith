namespace ProjectZenith.Contracts.Infrastructure.Messaging
{
    public static class KafkaTopics
    {
        public const string Users = "users";
        public const string Developers = "developers";
        public const string Apps = "apps";
        public const string Purchases = "purchases";
        public const string Payouts = "payouts";
        public const string Reviews = "reviews";

        // Topics for processing file results
        // Keep them separate because they may have different processing logic or consumers
        public const string AppFileProcessingResults = "app-file-processing-results";
        public const string ScreenshotProcessingResults = "screenshot-processing-results";

    }
}
