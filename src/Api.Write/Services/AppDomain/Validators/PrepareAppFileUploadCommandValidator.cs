using FluentValidation;
using Microsoft.Extensions.Options;
using ProjectZenith.Contracts.Commands.App;
using ProjectZenith.Contracts.Configuration;

namespace ProjectZenith.Api.Write.Services.AppDomain.Validators
{
    public class PrepareAppFileUploadCommandValidator : AbstractValidator<PrepareAppFileUploadCommand>
    {
        public PrepareAppFileUploadCommandValidator(IOptions<BlobStorageOptions> blobStorageOptions)
        {
            var maxScreenshots = blobStorageOptions.Value.MaxScreenshots;
            var maxScreenshotSize = blobStorageOptions.Value.MaxScreenshotSize;

            RuleFor(x => x.DeveloperId)
                .NotEmpty().WithMessage("DeveloperId is required.");

            RuleFor(x => x.AppName)
                .NotEmpty().WithMessage("App name is required.")
                .MaximumLength(200).WithMessage("App name cannot exceed 200 characters.");

            RuleFor(x => x.VersionNumber)
                .NotEmpty().WithMessage("Version number is required.")
                .Matches(@"^\d+(\.\d+){0,2}$").WithMessage("Version number must follow semantic versioning (e.g., 1.0.0).");

            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("File name is required.")
                .MaximumLength(255).WithMessage("File name cannot exceed 255 characters.");

            RuleFor(x => x.FileSize)
                .GreaterThan(0).WithMessage("File size must be greater than zero.")
                .LessThanOrEqualTo(500 * 1024 * 1024).WithMessage("File size cannot exceed 500 MB.");

            RuleFor(x => x.ContentType)
                .NotEmpty().WithMessage("Content type is required.")
                .Matches(@"^[\w\-\+\.]+\/[\w\-\+\.]+$").WithMessage("Content type must be a valid MIME type (e.g., application/zip).");

            RuleFor(x => x.ScreenshotFileNames)
            .NotNull().WithMessage("Screenshot file names are required.")
            .Must(f => f.Count <= maxScreenshots).WithMessage($"Maximum {maxScreenshots} screenshots allowed.")
            .ForEach(f => f
                .NotEmpty().WithMessage("Screenshot file name cannot be empty.")
                .Length(1, 100).WithMessage("Screenshot file name must be 1-100 characters.")
                .Must(n => n.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                          n.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Screenshot file must be .png or .jpg."));
        }
    }
}
