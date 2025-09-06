namespace ProjectZenith.Contracts.Events.App
{
    public record AppVersionSubmittedEvent
    {
        public Guid AppId { get; init; }
        public Guid VersionId { get; init; }
        public Guid DeveloperId { get; init; }
        public string VersionNumber { get; init; } = string.Empty;
        public DateTime SubmittedAt { get; init; }
    }
}
