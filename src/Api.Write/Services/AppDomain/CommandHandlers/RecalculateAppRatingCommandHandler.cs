using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.App;

namespace ProjectZenith.Api.Write.Services.AppDomain.CommandHandlers
{
    public class RecalculateAppRatingCommandHandler : IRequestHandler<RecalculateAppRatingCommand, Unit>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IValidator<RecalculateAppRatingCommand> _validator;
        private readonly ILogger<RecalculateAppRatingCommandHandler> _logger;

        public RecalculateAppRatingCommandHandler(
            WriteDbContext dbContext,
            IValidator<RecalculateAppRatingCommand> validator,
            ILogger<RecalculateAppRatingCommandHandler> logger)
        {
            _dbContext = dbContext;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Unit> Handle(RecalculateAppRatingCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            var app = await _dbContext.Apps
                .FirstOrDefaultAsync(a => a.Id == command.AppId && !a.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException($"App with ID {command.AppId} not found or is deleted.");

            if (app == null)
            {
                _logger.LogWarning("App with ID {AppId} not found or is deleted.", command.AppId);
                return Unit.Value;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var allValidRatings = await _dbContext.Reviews
                    .Where(r => r.AppId == command.AppId && !r.IsDeleted)
                    .Select(r => r.Rating)
                    .ToListAsync(cancellationToken);

                if (allValidRatings.Any())
                {
                    app.RatingCount = allValidRatings.Count;
                    app.RatingSum = allValidRatings.Sum(r => r);
                    app.AverageRating = Math.Round((decimal)app.RatingSum / app.RatingCount, 2);
                }
                else
                {
                    app.RatingCount = 0;
                    app.RatingSum = 0;
                    app.AverageRating = 0;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Recalculated ratings for app {AppId}: Count={RatingCount}, Sum={RatingSum}, Average={AverageRating}",
                    app.Id, app.RatingCount, app.RatingSum, app.AverageRating);

                return Unit.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating ratings for app {AppId}: {Message}", command.AppId, ex.Message);
                throw;
            }
        }
    }
}
