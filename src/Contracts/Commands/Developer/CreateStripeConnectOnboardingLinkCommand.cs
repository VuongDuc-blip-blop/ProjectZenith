using MediatR;

namespace ProjectZenith.Contracts.Commands.Developer
{
    public record CreateStripeConnectOnboardingLinkCommand(Guid DeveloperId, string ReturnUrl, string RefreshUrl) : IRequest<string>;
}
