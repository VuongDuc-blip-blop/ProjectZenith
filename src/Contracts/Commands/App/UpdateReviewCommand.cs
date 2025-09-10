using MediatR;

namespace ProjectZenith.Contracts.Commands.App
{
    public record UpdateReviewCommand(Guid UserId, Guid ReviewId, int Rating, string? Comment) : IRequest<Unit>;
}
