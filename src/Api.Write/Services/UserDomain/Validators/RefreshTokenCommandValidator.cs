using FluentValidation;
using ProjectZenith.Contracts.Commands.User;

namespace ProjectZenith.Api.Write.Services.UserDomain.Validators
{
    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.RefreshTokenId).NotEmpty()
                .WithMessage("Refresh token is required");
            RuleFor(x => x.RefreshToken).NotEmpty()
                .WithMessage("Refresh token is required");
        }
    }
}
