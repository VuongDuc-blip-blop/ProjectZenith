using FluentValidation;
using ProjectZenith.Contracts.Commands.App;

namespace ProjectZenith.Api.Write.Services.AppDomain.Validators
{
    public class MarkAppAsPendingApprovalCommandValidator : AbstractValidator<MarkAppAsPendingApprovalCommand>
    {
        public MarkAppAsPendingApprovalCommandValidator()
        {
            RuleFor(x => x.AppId)
                .NotEmpty().WithMessage("App ID is required.");

            RuleFor(x => x.AppFileId)
                .NotEmpty().WithMessage("App File ID is required.");

            RuleFor(x => x.FinalPath)
                .NotEmpty().WithMessage("Final path is required.")
                .Length(1, 500).WithMessage("Final path must be 1-500 characters.");
        }
    }
}
