using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.App;
using ProjectZenith.Contracts.Enums;

namespace ProjectZenith.Api.Write.Services.AppDomain.CommandHandlers
{
    public class MarkAppAsPendingApprovalCommandHandler : IRequestHandler<MarkAppAsPendingApprovalCommand, Unit>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IValidator<MarkAppAsPendingApprovalCommand> _validator;
        private readonly ILogger<MarkAppAsPendingApprovalCommandHandler> _logger;

        public MarkAppAsPendingApprovalCommandHandler(WriteDbContext dbContext, IValidator<MarkAppAsPendingApprovalCommand> validator, ILogger<MarkAppAsPendingApprovalCommandHandler> logger)
        {
            _dbContext = dbContext;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Unit> Handle(MarkAppAsPendingApprovalCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken: cancellationToken);

            var version = await _dbContext.AppVersions
            .Include(v => v.File)
            .FirstOrDefaultAsync(v => v.AppId == command.AppId && v.FileId == command.AppFileId && v.Status == Status.PendingValidation, cancellationToken)
            ?? throw new InvalidOperationException($"Version for App {command.AppId} and File {command.AppFileId} not found or not in PendingValidation status.");

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
                _logger.LogError(ex, "Failed to mark version for app {AppId} as PendingApproval: {Message}", command.AppId, ex.Message);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

}
