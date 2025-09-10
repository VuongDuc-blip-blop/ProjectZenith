using MediatR;

namespace ProjectZenith.Contracts.Commands.Developer
{
    public record ProcessStripeAccountUpdateCommand(string StripeAccountId, bool PayoutsEnabled, bool ChargesEnabled, bool DetailsSubmitted) : IRequest<Unit>;
}
