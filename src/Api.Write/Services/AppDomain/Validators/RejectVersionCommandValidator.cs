using FluentValidation;
using ProjectZenith.Contracts.Commands.App;

namespace ProjectZenith.Api.Write.Services.AppDomain.Validators
{
    public class RejectVersionCommandValidator : AbstractValidator<RejectVersionCommand>
    {
        public RejectVersionCommandValidator()
        {
            RuleFor(x => x.AppId).NotEmpty().WithMessage("App ID is required.");
            RuleFor(x => x.VersionId).NotEmpty().WithMessage("Version ID is required.");
            RuleFor(x => x.AppFileId).NotEmpty().WithMessage("App File ID is required.");
            RuleFor(x => x.Reason).NotEmpty().Length(1, 1000).WithMessage("Reason must be 1-1000 characters.");
            RuleFor(x => x.RejectedPath).NotEmpty().Length(1, 500).WithMessage("Rejected path must be 1-500 characters.");
        }
    }
}
