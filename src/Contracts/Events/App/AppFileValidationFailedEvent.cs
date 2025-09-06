namespace ProjectZenith.Contracts.Events.App
{
    public record AppFileValidationFailedEvent
    {
        public string BlobName { get; init; } = string.Empty;
        public Guid AppId { get; init; }
        public Guid AppFileId { get; init; }
        public string Reason { get; init; } = string.Empty;
        public string RejectedPath { get; init; } = string.Empty;
    }
}
