using FluentValidation;
using ProjectZenith.Contracts.Commands.App;

namespace ProjectZenith.Api.Write.Services.AppDomain.Validators
{
    public class MarkScreenshotProcessedCommandValidator : AbstractValidator<MarkScreenshotProcessedCommand>
    {
        public MarkScreenshotProcessedCommandValidator()
        {
            RuleFor(x => x.AppId)
                .NotEmpty().WithMessage("App ID is required.");

            RuleFor(x => x.ScreenshotId)
                .NotEmpty().WithMessage("Screenshot ID is required.");

            RuleFor(x => x.BlobName)
                .NotEmpty().WithMessage("Blob name is required.")
                .Length(1, 500).WithMessage("Blob name must be 1-500 characters.");

            RuleFor(x => x.Checksum)
                .NotEmpty().WithMessage("Checksum is required.")
                .Matches("^[a-fA-F0-9]{64}$").WithMessage("Invalid SHA256 checksum format.");
        }
    }
}
