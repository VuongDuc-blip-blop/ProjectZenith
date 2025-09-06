using FluentValidation;
using ProjectZenith.Contracts.Commands.App;

namespace ProjectZenith.Api.Write.Services.AppDomain.Validators
{
    public class UnpublishVersionCommandValidator : AbstractValidator<UnpublishVersionCommand>
    {
        public UnpublishVersionCommandValidator()
        {
            RuleFor(x => x.AppId).NotEmpty().WithMessage("App ID is required.");
            RuleFor(x => x.VersionId).NotEmpty().WithMessage("Version ID is required.");
        }
    }
}
