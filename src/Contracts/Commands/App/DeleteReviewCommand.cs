using MediatR;

namespace ProjectZenith.Contracts.Commands.App
{
    public record DeleteReviewCommand(Guid UserId, Guid ReviewId) : IRequest<Unit>;
}
