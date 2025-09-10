using FluentValidation;
using ProjectZenith.Contracts.Commands.App;

namespace ProjectZenith.Api.Write.Services.AppDomain.Validators
{
    public class DeleteAppCommandValidator : AbstractValidator<DeleteAppCommand>
    {
        public DeleteAppCommandValidator()
        {
            RuleFor(x => x.AppId).NotEmpty().WithMessage("App ID is required.");
            RuleFor(x => x.DeveloperId).NotEmpty().WithMessage("Developer ID is required.");
        }
    }

}
