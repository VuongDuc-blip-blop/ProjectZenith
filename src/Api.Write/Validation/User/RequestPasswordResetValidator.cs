using FluentValidation;
using ProjectZenith.Contracts.Commands;
namespace ProjectZenith.Api.Write.Validation.User
{
    public class RequestPasswordResetValidator : AbstractValidator<RequestPasswordResetCommand>
    {
        public RequestPasswordResetValidator()
        {
            RuleFor(x => x.Email).NotEmpty()
                .WithMessage("Email is required");
        }
    }
}
