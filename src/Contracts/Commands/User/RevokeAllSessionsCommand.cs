using MediatR;

namespace ProjectZenith.Contracts.Commands.User
{
    /// <summary>
    /// Command to revoke all sessions for a user.
    /// </summary>
    public record RevokeAllSessionsCommand : IRequest
    {
        /// <summary>
        /// The unique identifier of the user whose sessions are to be revoked.
        /// </summary>
        public Guid UserId { get; init; }
    }
}
