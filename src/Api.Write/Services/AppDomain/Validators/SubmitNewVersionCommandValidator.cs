using FluentValidation;
using Microsoft.Extensions.Options;
using ProjectZenith.Contracts.Commands.App;
using ProjectZenith.Contracts.Configuration;
using System.Text.RegularExpressions;

namespace ProjectZenith.Api.Write.Services.AppDomain.Validators
{
    public class SubmitNewVersionCommandValidator : AbstractValidator<SubmitNewVersionCommand>
    {
        public SubmitNewVersionCommandValidator(IOptions<BlobStorageOptions> options)
        {
            var maxScreenshots = options.Value.MaxScreenshots;
            var maxTags = options.Value.MaxTags;
            var maxScreenshotSize = options.Value.MaxScreenshotSize;

            RuleFor(x => x.DeveloperId).NotEmpty().WithMessage("Developer ID is required.");
            RuleFor(x => x.AppId).NotEmpty().WithMessage("App ID is required.");
            RuleFor(x => x.SubmissionId).NotEmpty().WithMessage("Submission ID is required.");
            RuleFor(x => x.VersionNumber).NotEmpty().Length(1, 50).WithMessage("Version number must be 1-50 characters.");
            RuleFor(x => x.Changelog).MaximumLength(2000).WithMessage("Changelog cannot exceed 2000 characters.");
            RuleFor(x => x.MainAppFileName).NotEmpty().Must(n => n.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).WithMessage("Main app file must be a .zip.");
            RuleFor(x => x.MainAppChecksum).NotEmpty().Matches("^[a-fA-F0-9]{64}$").WithMessage("Invalid SHA256 checksum format.");
            RuleFor(x => x.MainAppFileSize).GreaterThan(0).LessThanOrEqualTo(500 * 1024 * 1024).WithMessage("File size must be 1-500 MB.");
            RuleFor(x => x.Screenshots).NotNull().Must(s => s.Count <= maxScreenshots).WithMessage($"Maximum {maxScreenshots} screenshots allowed.")
                .ForEach(s => s
                    .Must(s => s.Size <= maxScreenshotSize).WithMessage($"Screenshot size must not exceed {maxScreenshotSize} bytes.")
                    .Must(s => Regex.IsMatch(s.Checksum, @"^[a-fA-F0-9]{64}$")).WithMessage("Invalid SHA256 checksum format.")
                    .Must(s => s.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || s.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                    .WithMessage("Screenshot file must be .png or .jpg."));
            RuleFor(x => x.Tags).NotNull().Must(t => t.Count <= maxTags).WithMessage($"Maximum {maxTags} tags allowed.")
                .ForEach(t => t
                    .NotEmpty().WithMessage("Tag cannot be empty.")
                    .Length(1, 50).WithMessage("Tag must be 1-50 characters.")
                    .Matches("^[a-zA-Z0-9-]+$").WithMessage("Tag must be alphanumeric with hyphens."));
        }
    }
}
