using FluentValidation;
using ProjectZenith.Contracts.Commands;
namespace ProjectZenith.Api.Write.Validation.User
{
    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.ResetToken).NotEmpty()
                .WithMessage("Reset token is required");
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Password is required.")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{8,}$")
                .WithMessage("Password must be at least 8 characters long, contain at least one uppercase letter, one lowercase letter, and one number.");
        }
    }
}
