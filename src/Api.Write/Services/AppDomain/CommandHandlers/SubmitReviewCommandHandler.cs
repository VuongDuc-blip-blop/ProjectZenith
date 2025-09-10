using FluentValidation;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Commands.App;
using ProjectZenith.Contracts.Enums;
using ProjectZenith.Contracts.Events.App;
using ProjectZenith.Contracts.Exceptions.App;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Infrastructure.Messaging;
using ProjectZenith.Contracts.Models;

namespace ProjectZenith.Api.Write.Services.AppDomain.CommandHandlers
{
    public class SubmitReviewCommandHandler : IRequestHandler<SubmitReviewCommand, Guid>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IValidator<SubmitReviewCommand> _validator;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<SubmitReviewCommandHandler> _logger;

        public SubmitReviewCommandHandler(
            WriteDbContext dbContext,
            IValidator<SubmitReviewCommand> validator,
            IEventPublisher eventPublisher,
            ILogger<SubmitReviewCommandHandler> logger)
        {
            _dbContext = dbContext;
            _validator = validator;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task<Guid> Handle(SubmitReviewCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            var app = await _dbContext.Apps
                .FirstOrDefaultAsync(a => a.Id == command.AppId && !a.IsDeleted && a.AppStatus == AppStatus.Active, cancellationToken)
                ?? throw new InvalidOperationException($"App with ID {command.AppId} not found or is not active.");

            var hasPurchased = await _dbContext.Purchases
                .AnyAsync(p => p.AppId == command.AppId && p.UserId == command.UserId && p.Status == PurchaseStatus.Completed && !p.IsDeleted, cancellationToken);

            if (!hasPurchased)
            {
                throw new UnauthorizedAccessException($"User with ID {command.UserId} has not purchased the app with ID {command.AppId}.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Check if user has already reviewed
                var existingReview = await _dbContext.Reviews
                    .FirstOrDefaultAsync(r => r.UserId == command.UserId && r.AppId == command.AppId && !r.IsDeleted, cancellationToken);

                if (existingReview != null)
                {
                    throw new DuplicateReviewException($"A review by UserId {command.UserId} for AppId {command.AppId} already exists. Please use the update endpoint to modify the review.");
                }


                // If not, create a new review
                var newReview = new Review
                {
                    Id = Guid.NewGuid(),
                    AppId = command.AppId,
                    UserId = command.UserId,
                    Rating = command.Rating,
                    Comment = command.Comment,
                    PostedAt = DateTime.UtcNow,
                    IsEdited = false
                };

                _dbContext.Reviews.Add(newReview);

                await _dbContext.SaveChangesAsync(cancellationToken);


                var submittedEvent = new ReviewSubmittedEvent
                {
                    ReviewId = newReview.Id,
                    AppId = newReview.AppId,
                    UserId = (Guid)newReview.UserId,
                    Rating = newReview.Rating,
                    Comment = newReview.Comment,
                    SubmittedAt = newReview.PostedAt
                };
                await _eventPublisher.PublishAsync(KafkaTopics.Reviews, newReview.AppId.ToString(), submittedEvent, cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return newReview.Id;

            }
            catch (DbUpdateException dbex) when (dbex.InnerException != null && dbex.InnerException is SqlException ex && (ex.Number == 2627 || ex.Number == 2601)) // Unique constraint error
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("A concurrency conflict occurred while submitting review for AppId: {AppId} by UserId: {UserId}. Error: {Error}", command.AppId, command.UserId, ex.Message);
                throw new InvalidOperationException("A concurrency conflict occurred. Please try submitting your review again.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred while submitting review for AppId: {AppId} by UserId: {UserId}, Message: {Message}", command.AppId, command.UserId, ex.Message);
                throw;

            }
        }

    }
}
