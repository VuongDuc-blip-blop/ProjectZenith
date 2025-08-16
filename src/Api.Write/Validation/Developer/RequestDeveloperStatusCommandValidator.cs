using FluentValidation;
using ProjectZenith.Contracts.Commands.User;
namespace ProjectZenith.Api.Write.Validation.Developer
{
    public class RequestDeveloperStatusCommandValidator : AbstractValidator<RequestDeveloperStatusCommand>
    {
        public RequestDeveloperStatusCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required.");


            RuleFor(x => x.ContactEmail)
                .NotEmpty().WithMessage("Contact email is required.")
                .EmailAddress().WithMessage("Contact email must be a valid email address.");
        }
    }
}
