using FluentValidation;
using ProjectZenith.Contracts.Commands.User;
namespace ProjectZenith.Api.Write.Services.UserDomain.Validators
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
