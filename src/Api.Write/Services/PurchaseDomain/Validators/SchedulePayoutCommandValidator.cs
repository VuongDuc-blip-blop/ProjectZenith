using FluentValidation;
using ProjectZenith.Contracts.Commands.Purchase;

namespace ProjectZenith.Api.Write.Services.PurchaseDomain.Validators
{
    public class SchedulePayoutCommandValidator : AbstractValidator<SchedulePayoutCommand>
    {
        public SchedulePayoutCommandValidator()
        {
            RuleFor(x => x.DeveloperId).NotEmpty().WithMessage("Developer ID is required.");
            RuleFor(x => x.PurchaseId).NotEmpty().WithMessage("Purchase ID is required.");
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be positive.")
                .LessThanOrEqualTo(9999999999999999.99m).WithMessage("Amount cannot exceed 18 digits with 2 decimal places.")
                .PrecisionScale(18, 2, false).WithMessage("Amount must have at most 2 decimal places.");
            RuleFor(x => x.PaymentProvider).NotEmpty().Must(p => p == "Stripe").WithMessage("Payment provider must be Stripe.");
            RuleFor(x => x.PaymentId).NotEmpty().WithMessage("Payment ID is required.");
        }
    }
}
