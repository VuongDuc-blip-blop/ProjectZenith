using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.Extensions.Options;
using ProjectZenith.Contracts.Commands.App;
using ProjectZenith.Contracts.Configuration;

namespace ProjectZenith.Api.Write.Services.AppDomain.Validators
{
    public class FinalizeAppSubmissionCommandValidator : AbstractValidator<FinalizeAppSubmissionCommand>
    {
        public FinalizeAppSubmissionCommandValidator(IOptions<BlobStorageOptions> options)
        {

            var maxScreenshotCount = options.Value.MaxScreenshots;
            var maxTags = options.Value.MaxTags;
            var maxScreenshotSize = options.Value.MaxScreenshotSize;

            RuleFor(x => x.DeveloperId)
                .NotEmpty().WithMessage("DeveloperId is required.");

            RuleFor(x => x.SubmissionId)
                .NotEmpty().WithMessage("Submission ID is required.");

            RuleFor(x => x.AppName)
                .NotEmpty().WithMessage("App name is required.")
                .MaximumLength(100).WithMessage("App name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Category is required.")
                .MaximumLength(100).WithMessage("Category cannot exceed 50 characters.");

            RuleFor(x => x.Platform)
                .NotEmpty().WithMessage("Platform is required.");


            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Price must be non-negative.");

            RuleFor(x => x.VersionNumber)
                .NotEmpty().WithMessage("Version number is required.")
                .MaximumLength(50).WithMessage("Version number cannot exceed 50 characters.");

            RuleFor(x => x.Changelog)
                .MaximumLength(2000).WithMessage("Changelog cannot exceed 2000 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Changelog));

            // File info
            RuleFor(x => x.MainAppFileName)
                .NotEmpty().WithMessage("Blob name is required.")
                .MaximumLength(500).WithMessage("Blob name cannot exceed 500 characters.");

            RuleFor(x => x.MainAppChecksum)
                .NotEmpty().WithMessage("Checksum is required.")
                .Matches("^[a-fA-F0-9]+$").WithMessage("Checksum must be a valid hex string.")
                .Length(64).WithMessage("Checksum must be 64 characters (SHA-256).");

            RuleFor(x => x.MainAppFileSize)
                .GreaterThan(0).WithMessage("File size must be greater than zero.");

            RuleFor(x => x.Screenshots)
            .NotNull().WithMessage("Screenshots are required.")
            .Must(s => s.Count <= maxScreenshotCount).WithMessage($"Maximum {maxScreenshotCount} screenshots allowed.")
            .ForEach(s => s
                .Must(s => s.Size <= maxScreenshotSize).WithMessage($"Screenshot size must not exceed {maxScreenshotSize} bytes.")
                .Must(s => Regex.IsMatch(s.Checksum, @"^[a-fA-F0-9]{64}$")).WithMessage("Invalid SHA256 checksum format.")
                .Must(s => !string.IsNullOrEmpty(s.FileName)).WithMessage("Screenshot file name cannot be empty.")
                .Must(s => s.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                          s.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Screenshot file must be .png or .jpg."));

            RuleFor(x => x.Tags)
            .NotNull().WithMessage("Tags are required.")
            .Must(t => t.Count <= maxTags).WithMessage($"Maximum {maxTags} tags allowed.")
            .ForEach(t => t
                .NotEmpty().WithMessage("Tag cannot be empty.")
                .Length(1, 50).WithMessage("Tag must be 1-50 characters.")
                .Matches("^[a-zA-Z0-9-]+$").WithMessage("Tag must be alphanumeric with hyphens."));
        }
    }
}
