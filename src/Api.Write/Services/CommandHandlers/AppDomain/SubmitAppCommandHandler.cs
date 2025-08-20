
using FluentValidation;
using MediatR;
using ProjectZenith.Contracts.Commands.App;
using System.Security.Cryptography;

namespace ProjectZenith.Api.Write.Services.CommandHandlers.AppDomain
{
    public class SubmitAppCommandHandler : IRequestHandler<SubmitAppCommand, Guid>
    {
        private readonly ILogger<SubmitAppCommandHandler> _logger;
        private readonly IValidator<SubmitAppCommand> _validator;

        public SubmitAppCommandHandler(ILogger<SubmitAppCommandHandler> logger, IValidator<SubmitAppCommand> validator)
        {
            _logger = logger;
            _validator = validator;
        }

        public async Task<Guid> Handle(SubmitAppCommand command, CancellationToken cancellationToken)
        {
            // 1. Run the validation rules from our new validator class.
            // This will throw a ValidationException if any rule fails.
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            _logger.LogInformation("Command for app '{AppName}' passed validation.", command.Name);

            // 2. Perform Metadata Parsing (Checksum Calculation)
            string checksum;
            long fileSize = command.AppFile.Length;

            using (var fileStream = command.AppFile.OpenReadStream())
            {
                using var sha256 = SHA256.Create();
                var hashBytes = await sha256.ComputeHashAsync(fileStream, cancellationToken);
                checksum = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }

            _logger.LogInformation(
                "File metadata parsed for '{FileName}'. Size: {FileSize} bytes, SHA256 Checksum: {Checksum}",
                command.AppFile.FileName,
                fileSize,
                checksum);


            var newAppId = Guid.NewGuid();
            _logger.LogInformation("SKELETON HANDLER: Returning mock AppId {AppId}", newAppId);

            return newAppId;
        }
    }
}
