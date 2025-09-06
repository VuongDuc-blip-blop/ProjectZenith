using FluentValidation;
using ProjectZenith.Contracts.Commands.User;
namespace ProjectZenith.Api.Write.Services.UserDomain.Validators
{
    public class RevokeAllSessionsCommandValidator : AbstractValidator<RevokeAllSessionsCommand>
    {
        public RevokeAllSessionsCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty()
                .WithMessage("User ID is required");
        }
    }
}
