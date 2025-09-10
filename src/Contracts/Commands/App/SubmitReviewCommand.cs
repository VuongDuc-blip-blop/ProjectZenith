using MediatR;

namespace ProjectZenith.Contracts.Commands.App
{
    public record SubmitReviewCommand(Guid UserId, Guid AppId, int Rating, string? Comment) : IRequest<Guid>;
}
