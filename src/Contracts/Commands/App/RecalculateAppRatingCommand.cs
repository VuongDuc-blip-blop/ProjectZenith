using MediatR;

namespace ProjectZenith.Contracts.Commands.App
{
    public record RecalculateAppRatingCommand(Guid AppId) : IRequest<Unit>;
}
