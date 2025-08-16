using FluentValidation;
using ProjectZenith.Contracts.Commands.User;
namespace ProjectZenith.Api.Write.Validation.User
{
    public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
    {
        public LogoutCommandValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty()
                .WithMessage("Refresh is required");
        }
    }
}
