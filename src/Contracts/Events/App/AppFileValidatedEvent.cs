namespace ProjectZenith.Contracts.Events.App
{
    public record AppFileValidatedEvent
    {
        public string BlobName { get; init; } = string.Empty;
        public string FinalPath { get; init; } = string.Empty;
        public Guid AppId { get; init; }
        public Guid AppFileId { get; init; }
        public string Checksum { get; init; } = string.Empty;
    }
}
