using MediatR;

namespace ProjectZenith.Contracts.Commands.Purchase
{
    public record CreatePurchaseCommand(Guid UserId, Guid AppId, decimal Price, string PaymentMethodId, string PaymentProvider) : IRequest<Guid>;
}
