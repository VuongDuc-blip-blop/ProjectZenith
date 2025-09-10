using FluentValidation;
using ProjectZenith.Contracts.Commands.Developer;

namespace ProjectZenith.Api.Write.Services.DeveloperDomain.Validators
{
    public class CreateStripeConnectOnboardingLinkCommandValidator : AbstractValidator<CreateStripeConnectOnboardingLinkCommand>
    {
        public CreateStripeConnectOnboardingLinkCommandValidator()
        {
            RuleFor(x => x.DeveloperId).NotEmpty().WithMessage("Developer ID is required.");
            RuleFor(x => x.ReturnUrl).NotEmpty().Must(BeValidUrl).WithMessage("Return URL must be a valid URL.");
            RuleFor(x => x.RefreshUrl).NotEmpty().Must(BeValidUrl).WithMessage("Refresh URL must be a valid URL.");
        }

        private bool BeValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
