using FluentValidation;
using ProjectZenith.Contracts.Commands;
namespace ProjectZenith.Api.Write.Validation.User
{
    public class PasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
    {
        public PasswordResetCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty()
                .WithMessage("Email is required");
        }
    }
}
