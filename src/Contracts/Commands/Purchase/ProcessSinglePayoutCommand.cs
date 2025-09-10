using MediatR;

namespace ProjectZenith.Contracts.Commands.Purchase
{
    public record ProcessSinglePayoutCommand(Guid PayoutId) : IRequest<Unit>;
}
