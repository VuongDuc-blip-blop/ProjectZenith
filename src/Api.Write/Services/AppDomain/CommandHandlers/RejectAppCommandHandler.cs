using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.App;
using ProjectZenith.Contracts.Enums;

namespace ProjectZenith.Api.Write.Services.AppDomain.CommandHandlers
{
    public class RejectAppCommandHandler : IRequestHandler<RejectAppCommand, Unit>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IValidator<RejectAppCommand> _validator;
        private readonly ILogger<RejectAppCommandHandler> _logger;

        public RejectAppCommandHandler(
            WriteDbContext dbContext,
            IValidator<RejectAppCommand> validator,
            ILogger<RejectAppCommandHandler> logger)
        {
            _dbContext = dbContext;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Unit> Handle(RejectAppCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken: cancellationToken);

            var version = await _dbContext.AppVersions
            .Include(v => v.File)
            .FirstOrDefaultAsync(v => v.AppId == command.AppId && v.FileId == command.AppFileId && v.Status == Status.PendingValidation, cancellationToken)
            ?? throw new InvalidOperationException($"Version for App {command.AppId} and File {command.AppFileId} not found or not in PendingValidation status.");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                version.Status = Status.Rejected;
                version.StatusReason = command.Reason;
                version.File.Path = command.RejectedPath;
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Unit.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reject version for app {AppId}: {Message}", command.AppId, ex.Message);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
