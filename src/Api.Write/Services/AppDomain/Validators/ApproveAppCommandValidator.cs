using FluentValidation;
using ProjectZenith.Contracts.Commands.App;

namespace ProjectZenith.Api.Write.Services.AppDomain.Validators
{
    public class ApproveAppCommandValidator : AbstractValidator<ApproveAppCommand>
    {
        public ApproveAppCommandValidator()
        {
            RuleFor(x => x.AppId).NotEmpty().WithMessage("App ID is required.");
            RuleFor(x => x.VersionId).NotEmpty().WithMessage("Version ID is required.");
        }
    }
}
