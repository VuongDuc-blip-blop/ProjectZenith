namespace ProjectZenith.Contracts.Events.App
{
    public record AppVersionUnpublishedEvent
    {
        public Guid AppId { get; init; }
        public Guid VersionId { get; init; }
        public string AppName { get; init; } = string.Empty;
        public string VersionNumber { get; init; } = string.Empty;
        public DateTime UnpublishedAt { get; init; }
    }
}
