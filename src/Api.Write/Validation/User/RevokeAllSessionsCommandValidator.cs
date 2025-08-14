using FluentValidation;
using ProjectZenith.Contracts.Commands;
namespace ProjectZenith.Api.Write.Validation.User
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
