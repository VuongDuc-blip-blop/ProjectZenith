namespace ProjectZenith.Contracts.Events.App
{
    public record AppVersionApprovedEvent
    {
        public Guid AppId { get; init; }
        public Guid VersionId { get; init; }
        public Guid DeveloperId { get; init; }
        public string AppName { get; init; } = string.Empty;
        public string VersionNumber { get; init; } = string.Empty;
        public DateTime ApprovedAt { get; init; }
    }
}
