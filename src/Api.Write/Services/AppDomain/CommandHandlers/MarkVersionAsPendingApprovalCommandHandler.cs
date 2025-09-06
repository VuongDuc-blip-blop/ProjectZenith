using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.App;
using ProjectZenith.Contracts.Enums;

namespace ProjectZenith.Api.Write.Services.AppDomain.CommandHandlers
{
    public class MarkVersionAsPendingApprovalCommandHandler : IRequestHandler<MarkVersionAsPendingApprovalCommand, Unit>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IValidator<MarkVersionAsPendingApprovalCommand> _validator;
        private readonly ILogger<MarkVersionAsPendingApprovalCommandHandler> _logger;

        public MarkVersionAsPendingApprovalCommandHandler(
            WriteDbContext dbContext,
            IValidator<MarkVersionAsPendingApprovalCommand> validator,
            ILogger<MarkVersionAsPendingApprovalCommandHandler> logger)
        {
            _dbContext = dbContext;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Unit> Handle(MarkVersionAsPendingApprovalCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken: cancellationToken);

            var version = await _dbContext.AppVersions
                .Include(v => v.File)
                .FirstOrDefaultAsync(v => v.Id == command.VersionId && v.Status == Status.PendingValidation, cancellationToken)
                ?? throw new InvalidOperationException($"Version with ID {command.VersionId} not found or not in PendingValidation status.");

            if (version.FileId != command.AppFileId)
            {
                throw new InvalidOperationException($"Version's FileId {version.FileId} does not match the provided AppFileId {command.AppFileId}.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                version.Status = Status.PendingApproval;
                version.File.Path = command.FinalPath;
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Unit.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark version {VersionId} as PendingApproval: {Message}", command.VersionId, ex.Message);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
