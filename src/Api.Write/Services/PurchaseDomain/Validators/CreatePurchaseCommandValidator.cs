using FluentValidation;
using ProjectZenith.Contracts.Commands.Purchase;

namespace ProjectZenith.Api.Write.Services.PurchaseDomain.Validators
{
    public class CreatePurchaseCommandValidator : AbstractValidator<CreatePurchaseCommand>
    {
        public CreatePurchaseCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
            RuleFor(x => x.AppId).NotEmpty().WithMessage("App ID is required.");
            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be positive.")
                .LessThanOrEqualTo(9999999999999999.99m).WithMessage("Price cannot exceed 18 digits with 2 decimal places.")
                .PrecisionScale(18, 2, false).WithMessage("Price must have at most 2 decimal places.");
            RuleFor(x => x.PaymentMethodId).NotEmpty().WithMessage("Payment Method ID is required.");
            RuleFor(x => x.PaymentProvider).NotEmpty().Must(p => p == "Stripe").WithMessage("Payment provider must be Stripe.");
        }
    }
}
