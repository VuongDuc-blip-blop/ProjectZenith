namespace ProjectZenith.Contracts.Events.App
{
    public record ReviewRepliedToEvent
    {
        public Guid ReviewId { get; init; }
        public Guid AppId { get; init; }
        public Guid DeveloperId { get; init; }
        public string ReplyContent { get; init; } = string.Empty;
        public DateTime RepliedAt { get; init; }
    }

}
