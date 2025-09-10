namespace ProjectZenith.Contracts.Events.App
{
    public record ReviewSubmittedEvent
    {
        public Guid ReviewId { get; init; }
        public Guid UserId { get; init; }
        public Guid AppId { get; init; }
        public int Rating { get; init; }
        public string? Comment { get; init; }
        public DateTime SubmittedAt { get; init; }
    }

}
