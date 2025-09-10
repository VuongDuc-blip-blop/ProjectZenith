using FluentValidation;
using ProjectZenith.Contracts.Commands.App;

namespace ProjectZenith.Api.Write.Services.AppDomain.Validators
{
    public class SetAppPriceCommandValidator : AbstractValidator<SetAppPriceCommand>
    {
        public SetAppPriceCommandValidator()
        {
            RuleFor(x => x.DeveloperId).NotEmpty().WithMessage("Developer ID is required.");
            RuleFor(x => x.AppId).NotEmpty().WithMessage("App ID is required.");
            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be positive.")
                .LessThanOrEqualTo(9999999999999999.99m).WithMessage("Price cannot exceed 18 digits with 2 decimal places.")
                .PrecisionScale(18, 2, false).WithMessage("Price must have at most 2 decimal places.");
        }
    }
}
