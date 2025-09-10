using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProjectZenith.Contracts.Commands.App;

namespace ProjectZenith.Api.Write.Controllers
{
    public class ReviewsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReviewsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPut("{reviewId}/reply")]
        public async Task<IActionResult> ReplyToReview(Guid reviewId, [FromBody] SubmitReviewReplyRequest request, CancellationToken cancellationToken)
        {
            var developerIdClaim = User.FindFirst("sub")?.Value
                ?? throw new UnauthorizedAccessException("Developer ID claim not found in token.");

            if (!Guid.TryParse(developerIdClaim, out var developerId))
                throw new UnauthorizedAccessException("Invalid Developer ID format in token.");

            var command = new SubmitReviewReplyCommand(developerId, reviewId, request.ReplyContent);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpPut("{reviewId}")]
        [EnableRateLimiting("ReviewActionsPolicy")] // Apply rate limiting to this endpoint
        public async Task<IActionResult> UpdateReview(
        Guid reviewId,
        [FromBody] UpdateReviewRequest request,
        CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst("sub")?.Value
                ?? throw new UnauthorizedAccessException("User ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new InvalidOperationException("Invalid User ID format in token.");

            var command = new UpdateReviewCommand(userId, reviewId, request.Rating, request.Comment);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpDelete("{reviewId}")]
        public async Task<IActionResult> DeleteReview(
        Guid reviewId,
        CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst("sub")?.Value
                ?? throw new UnauthorizedAccessException("User ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new InvalidOperationException("Invalid User ID format in token.");

            var command = new DeleteReviewCommand(userId, reviewId);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }



    }
    public record SubmitReviewReplyRequest(string ReplyContent);
    public record UpdateReviewRequest(int Rating, string? Comment);
}
