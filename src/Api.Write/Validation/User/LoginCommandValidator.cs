using FluentValidation;
using ProjectZenith.Contracts.Commands.User;

namespace ProjectZenith.Api.Write.Validation.User
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");
            RuleFor(x => x.Password).NotEmpty()
                .WithMessage("Password is required");
        }
    }
}
