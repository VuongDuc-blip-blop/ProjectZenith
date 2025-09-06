using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Api.Write.Services.AppDomain.DomainServices;
using ProjectZenith.Contracts.Commands.App;
using ProjectZenith.Contracts.Enums;

namespace ProjectZenith.Api.Write.Services.AppDomain.CommandHandlers
{
    public class RejectVersionCommandHandler : IRequestHandler<RejectVersionCommand, Unit>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IAppStatusService _appStatusService;
        private readonly IValidator<RejectVersionCommand> _validator;
        private readonly ILogger<RejectVersionCommandHandler> _logger;

        public RejectVersionCommandHandler(
            WriteDbContext dbContext,
            IAppStatusService appStatusService,
            IValidator<RejectVersionCommand> validator,
            ILogger<RejectVersionCommandHandler> logger)
        {
            _dbContext = dbContext;
            _appStatusService = appStatusService;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Unit> Handle(RejectVersionCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            var version = await _dbContext.AppVersions
                .Include(v => v.File)
                .FirstOrDefaultAsync(v => v.Id == command.VersionId && v.AppId == command.AppId && v.Status == Status.PendingValidation, cancellationToken)
                ?? throw new InvalidOperationException($"Version {command.VersionId} for App {command.AppId} not found or not in PendingValidation status.");

            if (version.FileId != command.AppFileId)
            {
                throw new InvalidOperationException($"Version's FileId {version.FileId} does not match the provided AppFileId {command.AppFileId}.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                version.Status = Status.Rejected;
                version.StatusReason = command.Reason;
                version.File.Path = command.RejectedPath;



                await _appStatusService.UpdateAppStatusAsync(command.AppId, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Unit.Value;
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Failed to reject version {VersionId} for app {AppId}:{Message}", command.VersionId, command.AppId, ex.Message);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
