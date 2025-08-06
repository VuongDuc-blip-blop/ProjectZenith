using FluentValidation;
using ProjectZenith.Contracts.Commands;

namespace ProjectZenith.Api.Write.Validation.User
{
    public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
    {
        public VerifyEmailCommandValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token is required.");

        }
    }

}
