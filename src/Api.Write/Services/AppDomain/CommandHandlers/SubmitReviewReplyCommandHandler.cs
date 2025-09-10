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
    public class SubmitReviewReplyCommandHandler : IRequestHandler<SubmitReviewReplyCommand, Unit>
    {
        private readonly WriteDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidator<SubmitReviewReplyCommand> _validator;
        private readonly ILogger<SubmitReviewReplyCommandHandler> _logger;

        public SubmitReviewReplyCommandHandler(
            WriteDbContext dbContext,
            IEventPublisher eventPublisher,
            IValidator<SubmitReviewReplyCommand> validator,
            ILogger<SubmitReviewReplyCommandHandler> logger)
        {
            _dbContext = dbContext;
            _eventPublisher = eventPublisher;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Unit> Handle(SubmitReviewReplyCommand command, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);

            var review = await _dbContext.Reviews
                .Include(r => r.App)
                .FirstOrDefaultAsync(r => r.Id == command.ReviewId && !r.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException($"Review with ID {command.ReviewId} not found or deleted.");

            if (review.App.DeveloperId != command.DeveloperId)
            {
                throw new UnauthorizedAccessException($"Developer with ID {command.DeveloperId} is not authorized to reply to review {command.ReviewId}.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                review.DeveloperReply = command.ReplyContent;
                review.DeveloperRepliedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);

                var @event = new ReviewRepliedToEvent
                {
                    ReviewId = review.Id,
                    AppId = review.AppId,
                    DeveloperId = command.DeveloperId,
                    ReplyContent = command.ReplyContent,
                    RepliedAt = DateTime.UtcNow
                };

                await _eventPublisher.PublishAsync(KafkaTopics.Apps, review.AppId.ToString(), @event, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Developer with ID {DeveloperId} replied to review {ReviewId} for app {AppId}.", command.DeveloperId, command.ReviewId, review.AppId);

                return Unit.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while developer with ID {DeveloperId} was replying to review {ReviewId}, Message: {Message}", command.DeveloperId, command.ReviewId, ex.Message);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
