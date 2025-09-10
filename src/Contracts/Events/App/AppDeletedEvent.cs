namespace ProjectZenith.Contracts.Events.App
{
    public record AppDeletedEvent
    {
        public Guid AppId { get; init; }
        public DateTime DeletedAt { get; init; }
    }

}
