using FluentValidation;
using ProjectZenith.Contracts.Commands.Developer;

namespace ProjectZenith.Api.Write.Services.DeveloperDomain.Validators
{
    public class ProcessStripeAccountUpdateCommandValidator : AbstractValidator<ProcessStripeAccountUpdateCommand>
    {
        public ProcessStripeAccountUpdateCommandValidator()
        {
            RuleFor(x => x.StripeAccountId).NotEmpty().WithMessage("Stripe Account ID is required.");
        }
    }
}
