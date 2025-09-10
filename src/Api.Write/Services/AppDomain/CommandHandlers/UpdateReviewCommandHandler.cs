using FluentValidation;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.App;
using ProjectZenith.Contracts.Enums;
using ProjectZenith.Contracts.Events.App;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Infrastructure.Messaging;

namespace ProjectZenith.Api.Write.Services.AppDomain.CommandHandlers
{
    public class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand, Unit>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<UpdateReviewCommand> _validator;
        private readonly ILogger<UpdateReviewCommandHandler> _logger;

        public UpdateReviewCommandHandler(
            WriteDbContext dbContext,
            IEventPublisher eventPublisher,
            IValidator<UpdateReviewCommand> validator,
            ILogger<UpdateReviewCommandHandler> logger)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Unit> Handle(UpdateReviewCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            var review = await _dbContext.Reviews
                .Include(r => r.App)
                .FirstOrDefaultAsync(r => r.Id == command.ReviewId && !r.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException($"Review with ID {command.ReviewId} not found or deleted.");

            if (review.UserId != command.UserId)
            {
                throw new UnauthorizedAccessException($"User with ID {command.UserId} is not authorized to update review {command.ReviewId}.");
            }

            if (review.App.AppStatus != AppStatus.Active || review.App.IsDeleted)
            {
                throw new InvalidOperationException($"Cannot update review for an inactive or deleted app (AppId: {review.AppId}).");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                review.Rating = command.Rating;
                review.Comment = command.Comment;
                review.IsEdited = true;
                review.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);

                var @event = new ReviewUpdatedEvent
                {
                    ReviewId = review.Id,
                    AppId = review.AppId,
                    UserId = (Guid)review.UserId,
                    Rating = command.Rating,
                    Comment = command.Comment,
                    UpdatedAt = DateTime.UtcNow
                };
                await _eventPublisher.PublishAsync(KafkaTopics.Reviews, review.AppId.ToString(), @event, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("User with ID {UserId} updated review {ReviewId} for app {AppId}.", command.UserId, command.ReviewId, review.AppId);
                return Unit.Value;
            }
            catch (DbUpdateException dbex) when (dbex.InnerException != null && dbex.InnerException is SqlException ex && (ex.Number == 2627 || ex.Number == 2601)) // Unique constraint error
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("A concurrency conflict occurred while submitting review for AppId: {AppId} by UserId: {UserId}. Error: {Error}", review.AppId, command.UserId, ex.Message);
                throw new InvalidOperationException("A concurrency conflict occurred. Please try submitting your review again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review {ReviewId} for app {AppId} by user {UserId}, Message: {Message}.", command.ReviewId, review.AppId, command.UserId, ex.Message);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
