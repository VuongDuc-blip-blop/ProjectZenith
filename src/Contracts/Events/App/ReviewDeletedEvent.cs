namespace ProjectZenith.Contracts.Events.App
{
    public record ReviewDeletedEvent
    {
        public Guid ReviewId { get; init; }
        public Guid AppId { get; init; }
        public Guid UserId { get; init; }
        public DateTime DeletedAt { get; init; }
    }

}
