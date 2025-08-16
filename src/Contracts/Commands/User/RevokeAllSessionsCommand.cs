using MediatR;

namespace ProjectZenith.Contracts.Commands.User
{
    public record RevokeAllSessionsCommand :IRequest
    {
        public Guid UserId { get; init; }
    }
}
