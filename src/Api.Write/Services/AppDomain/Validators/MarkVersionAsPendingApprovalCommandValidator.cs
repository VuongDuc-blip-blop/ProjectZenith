using FluentValidation;
using ProjectZenith.Contracts.Commands.App;

namespace ProjectZenith.Api.Write.Services.AppDomain.Validators
{
    public class MarkVersionAsPendingApprovalCommandValidator : AbstractValidator<MarkVersionAsPendingApprovalCommand>
    {
        public MarkVersionAsPendingApprovalCommandValidator()
        {
            RuleFor(x => x.AppId).NotEmpty().WithMessage("App ID is required.");
            RuleFor(x => x.VersionId).NotEmpty().WithMessage("Version ID is required.");
            RuleFor(x => x.AppFileId).NotEmpty().WithMessage("App File ID is required.");
            RuleFor(x => x.FinalPath).NotEmpty().Length(1, 500).WithMessage("Final path must be 1-500 characters.");
        }
    }
}
