using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.App;
using ProjectZenith.Contracts.Events.App;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Infrastructure.Messaging;

namespace ProjectZenith.Api.Write.Services.AppDomain.CommandHandlers
{
    public class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand, Unit>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<DeleteReviewCommand> _validator;
        private readonly ILogger<DeleteReviewCommandHandler> _logger;

        public DeleteReviewCommandHandler(
            WriteDbContext dbContext,
            IEventPublisher eventPublisher,
            IValidator<DeleteReviewCommand> validator,
            ILogger<DeleteReviewCommandHandler> logger)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Unit> Handle(DeleteReviewCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            var review = await _dbContext.Reviews
                .FirstOrDefaultAsync(r => r.Id == command.ReviewId && !r.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException($"Review with ID {command.ReviewId} not found or already deleted.");

            var userIsOwner = review.UserId == command.UserId;
            var userIsAdmin = false;

            if (!userIsOwner && !userIsAdmin)
            {
                throw new UnauthorizedAccessException($"User with ID {command.UserId} is not authorized to delete review {command.ReviewId}.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                review.IsDeleted = true;
                review.DeletedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);

                var @event = new ReviewDeletedEvent
                {
                    ReviewId = review.Id,
                    AppId = review.AppId,
                    UserId = (Guid)review.UserId,
                    DeletedAt = DateTime.UtcNow
                };
                await _eventPublisher.PublishAsync(KafkaTopics.Reviews, review.AppId.ToString(), @event, cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Review with ID {ReviewId} deleted by User ID {UserId}", review.Id, command.UserId);
                return Unit.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review with ID {ReviewId} by User ID {UserId}, Message: {Message}", review.Id, command.UserId, ex.Message);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

        }
    }
}
