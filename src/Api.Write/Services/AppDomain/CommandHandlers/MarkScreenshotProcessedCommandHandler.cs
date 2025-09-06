using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.App;

namespace ProjectZenith.Api.Write.Services.AppDomain.CommandHandlers
{
    public class MarkScreenshotProcessedCommandHandler : IRequestHandler<MarkScreenshotProcessedCommand, Unit>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IValidator<MarkScreenshotProcessedCommand> _validator;
        private readonly ILogger<MarkScreenshotProcessedCommandHandler> _logger;

        public MarkScreenshotProcessedCommandHandler(
            WriteDbContext dbContext,
            IValidator<MarkScreenshotProcessedCommand> validator,
            ILogger<MarkScreenshotProcessedCommandHandler> logger)
        {
            _dbContext = dbContext;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Unit> Handle(MarkScreenshotProcessedCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken: cancellationToken);

            var screenshot = await _dbContext.AppScreenshots
                                   .FirstOrDefaultAsync(s => s.Id == command.ScreenshotId && s.AppId == command.AppId, cancellationToken)
                                   ?? throw new InvalidOperationException($"Screenshot with ID {command.ScreenshotId} not found for App {command.AppId}");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                screenshot.Checksum = command.Checksum;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Unit.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking screenshot {ScreenshotId} as processed for App {AppId}", command.ScreenshotId, command.AppId);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
