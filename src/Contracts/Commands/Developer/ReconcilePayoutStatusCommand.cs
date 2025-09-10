using MediatR;

namespace ProjectZenith.Contracts.Commands.Developer
{
    public record ReconcilePayoutStatusCommand(Guid DeveloperId) : IRequest<Unit>;
}
