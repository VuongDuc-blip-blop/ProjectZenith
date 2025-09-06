using FluentValidation;
using ProjectZenith.Contracts.Commands.App;

namespace ProjectZenith.Api.Write.Services.AppDomain.Validators
{
    public class RejectAppCommandValidator : AbstractValidator<RejectAppCommand>
    {
        public RejectAppCommandValidator()
        {
            RuleFor(x => x.AppId)
                .NotEmpty().WithMessage("App ID is required.");

            RuleFor(x => x.AppFileId)
                .NotEmpty().WithMessage("App File ID is required.");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason is required.")
                .Length(1, 1000).WithMessage("Reason must be 1-1000 characters.");

            RuleFor(x => x.RejectedPath)
                .NotEmpty().WithMessage("Rejected path is required.")
                .Length(1, 500).WithMessage("Rejected path must be 1-500 characters.");
        }
    }
}
