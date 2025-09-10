using FluentValidation;
using ProjectZenith.Contracts.Commands.Purchase;

namespace ProjectZenith.Api.Write.Services.PurchaseDomain.Validators
{
    public class ProcessSinglePayoutCommandValidator : AbstractValidator<ProcessSinglePayoutCommand>
    {
        public ProcessSinglePayoutCommandValidator()
        {
            RuleFor(x => x.PayoutId).NotEmpty().WithMessage("Payout ID is required.");
        }
    }
}
