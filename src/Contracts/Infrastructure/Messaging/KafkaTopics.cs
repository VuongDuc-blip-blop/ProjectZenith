namespace ProjectZenith.Contracts.Infrastructure.Messaging
{
    public static class KafkaTopics
    {
        public const string UserEvents = "user-events";
        public const string DeveloperEvents = "developer-events";
        public const string AppEvents = "app-events";
        public const string AppFileResultEvents = "appfile-result-events";
        public const string ScreenshotResultEvents = "screenshot-result-events";
        public const string AppApprovedEvents = "app-approved-events";
        public const string AppNewVersionSubmittedEvents = "app-newversion-submitted-events";
        public const string AppVersionUnpublishedEvents = "app-version-unpublished-events";

    }
}
