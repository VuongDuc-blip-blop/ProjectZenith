using FluentValidation;
using ProjectZenith.Contracts.Commands.App;

namespace ProjectZenith.Api.Write.Services.AppDomain.Validators
{
    public class RecalculateAppRatingCommandValidator : AbstractValidator<RecalculateAppRatingCommand>
    {
        public RecalculateAppRatingCommandValidator()
        {
            RuleFor(x => x.AppId).NotEmpty().WithMessage("App ID is required.");
        }
    }

}
