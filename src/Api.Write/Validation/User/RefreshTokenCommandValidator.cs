using FluentValidation;
using ProjectZenith.Contracts.Commands;

namespace ProjectZenith.Api.Write.Validation.User
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
