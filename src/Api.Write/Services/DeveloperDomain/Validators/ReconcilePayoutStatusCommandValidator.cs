using FluentValidation;
using ProjectZenith.Contracts.Commands.Developer;

namespace ProjectZenith.Api.Write.Services.DeveloperDomain.Validators
{
    public class ReconcilePayoutStatusCommandValidator : AbstractValidator<ReconcilePayoutStatusCommand>
    {
        public ReconcilePayoutStatusCommandValidator()
        {
            RuleFor(x => x.DeveloperId).NotEmpty().WithMessage("Developer ID is required.");
        }
    }
}
