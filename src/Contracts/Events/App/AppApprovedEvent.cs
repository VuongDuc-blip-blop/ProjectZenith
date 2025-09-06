namespace ProjectZenith.Contracts.Events.App
{
    public record AppApprovedEvent(Guid AppId, Guid DeveloperId, string AppName, DateTime ApprovedAt);
}
