using MediatR;

namespace ProjectZenith.Contracts.Commands.Purchase
{
    public record SchedulePayoutCommand(Guid DeveloperId, Guid PurchaseId, decimal Amount, string PaymentProvider, string PaymentId) : IRequest<Guid>;
}
