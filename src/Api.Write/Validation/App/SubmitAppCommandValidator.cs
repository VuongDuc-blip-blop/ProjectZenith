using FluentValidation;
using ProjectZenith.Contracts.Commands.App;

namespace ProjectZenith.Api.Write.Validation.App
{
    public class SubmitAppCommandValidator : AbstractValidator<SubmitAppCommand>
    {
        // Define validation constants
        private const long MaxAppFileSize = 1 * 1024 * 1024 * 1024; // 1 GB
        private static readonly string[] AllowedAppContentTypes = { "application/zip", "application/x-msdownload" /* for .exe */ };

        public SubmitAppCommandValidator(IFileSignatureValidator fileSignatureValidator)
        {
            // Metadata Validation 
            RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Description).NotEmpty();
            RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Platform).NotEmpty().MaximumLength(50);
            RuleFor(x => x.VersionNumber).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0);

            // AppFile Validation with Magic Bytes
            RuleFor(x => x.AppFile)
                .NotNull().WithMessage("Application file is required.")
                .Must(BeWithinSizeLimit).WithMessage($"Application file size cannot exceed {MaxAppFileSize / 1024 / 1024} MB.")

                // Validate the file using magic bytes
                .Must(file => fileSignatureValidator.IsValidFileSignature(file))
                .WithMessage("The uploaded file is not a valid or supported format (Only .zip and .exe are allowed).");
        }

        private bool BeWithinSizeLimit(IFormFile file)
        {
            return file != null && file.Length > 0 && file.Length <= MaxAppFileSize;
        }

    }
}
