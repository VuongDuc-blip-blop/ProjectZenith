using FluentValidation;
using ProjectZenith.Contracts.Commands.User;

namespace ProjectZenith.Api.Write.Services.UserDomain.Validators
{
    public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
    {
        public UpdateUserProfileCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required.");

            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Display name is required.")
                .MaximumLength(50).WithMessage("Display name cannot exceed 100 characters.")
                .Matches(@"^[a-zA-Z0-9\s\-_]+$").WithMessage("Display name contains invalid characters.");

            RuleFor(x => x.Bio)
                .MaximumLength(500).WithMessage("Bio cannot exceed 500 characters.")
                .Matches(@"^[a-zA-Z0-9\s\.,!?'\-()]+$").When(x => x.Bio != null).WithMessage("Bio contains invalid characters.");
        }
    }
}
