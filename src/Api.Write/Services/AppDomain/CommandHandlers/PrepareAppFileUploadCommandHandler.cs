using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Commands.App;
using ProjectZenith.Contracts.DTOs.App;
using ProjectZenith.Contracts.Configuration;
using Microsoft.Extensions.Options;

namespace ProjectZenith.Api.Write.Services.AppDomain.CommandHandlers
{
    public class PrepareAppFileUploadCommandHandler : IRequestHandler<PrepareAppFileUploadCommand, PrepareUploadResponseDTO>
    {
        private readonly IBlobStorageService _blobStorageService;
        private readonly WriteDbContext _dbContext;
        private readonly IValidator<PrepareAppFileUploadCommand> _validator;
        private readonly BlobStorageOptions _blobStorageOptions;

        public PrepareAppFileUploadCommandHandler(
            IBlobStorageService blobStorageService,
            WriteDbContext dbContext,
            IValidator<PrepareAppFileUploadCommand> validator,
            IOptions<BlobStorageOptions> blobStorageOptions)
        {
            _blobStorageService = blobStorageService;
            _dbContext = dbContext;
            _validator = validator;
            _blobStorageOptions = blobStorageOptions.Value;
        }

        public async Task<PrepareUploadResponseDTO> Handle(
       PrepareAppFileUploadCommand command,
       CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            // 2. Check if the user exists and doesn't already have a developer profile.
            var developerExists = await _dbContext.Developers.AnyAsync(u => u.UserId == command.DeveloperId, cancellationToken);
            if (!developerExists)
            {
                throw new InvalidOperationException($"Developer with ID {command.DeveloperId} not found.");
            }

            var submissionId = Guid.NewGuid();
            var uploadPrefix = $"staging/{submissionId}";

            // Ask the blob service for a secure SAS URL
            var sasToken = await _blobStorageService.GetUserDelegationSasAsync(
                _blobStorageOptions.QuarantineContainerName // container
            );
            var uploadUrl = $"https://{_blobStorageOptions.AccountName}.blob.core.windows.net/{_blobStorageOptions.QuarantineContainerName}/{uploadPrefix}";

            return new PrepareUploadResponseDTO()
            {
                SubmissionId = submissionId,
                SecuredUploadUrl = uploadUrl,
                UploadSasToken = sasToken
            };
        }

    }
}
