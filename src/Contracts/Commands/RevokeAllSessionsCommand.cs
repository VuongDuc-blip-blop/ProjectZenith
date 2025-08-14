namespace ProjectZenith.Contracts.Commands
{
    public record RevokeAllSessionsCommand
    {
        public Guid UserId { get; init; }
    }
}
