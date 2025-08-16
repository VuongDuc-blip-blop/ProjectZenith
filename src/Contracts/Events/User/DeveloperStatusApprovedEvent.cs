namespace ProjectZenith.Contracts.Events.User
{
    public record DeveloperStatusApprovedEvent
    {
        public Guid UserId { get; init; }
        public DateTime ApprovedAt { get; init; }
        public Guid? ApprovedByAdminId { get; init; } = null;
    }
}
