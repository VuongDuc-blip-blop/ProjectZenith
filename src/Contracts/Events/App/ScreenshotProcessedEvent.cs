namespace ProjectZenith.Contracts.Events.App
{
    public record ScreenshotProcessedEvent
    {
        public string BlobName { get; init; } = string.Empty;
        public Guid AppId { get; init; }
        public Guid ScreenshotId { get; init; }
        public string Checksum { get; init; } = string.Empty;
    }
}
