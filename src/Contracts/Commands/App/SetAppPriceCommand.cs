using MediatR;

namespace ProjectZenith.Contracts.Commands.App
{
    public record SetAppPriceCommand(Guid DeveloperId, Guid AppId, decimal Price) : IRequest<Unit>;
}
