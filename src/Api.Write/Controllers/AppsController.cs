
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProjectZenith.Contracts.Commands.App;
using ProjectZenith.Contracts.Exceptions.App;
using System.Security.Claims;

namespace ProjectZenith.Api.Write.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Developer")]
    public class AppsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AppsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("prepare-upload")]
        public async Task<IActionResult> PrepareUpload([FromBody] PrepareAppFileUploadCommand command, CancellationToken cancellationToken)
        {
            // Security check: ensure the developer ID in the command matches the token
            var developerIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(developerIdString, out var developerId) || developerId != command.DeveloperId)
            {
                return Forbid();
            }

            var response = await _mediator.Send(command, cancellationToken);

            return Ok(response);
        }

        [HttpPost("finalize-submission")]
        public async Task<IActionResult> FinalizeSubmission([FromBody] FinalizeAppSubmissionCommand command, CancellationToken cancellationToken)
        {
            // Security check: ensure the developer ID in the command matches the token
            var developerIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(developerIdString, out var developerId) || developerId != command.DeveloperId)
            {
                return Forbid();
            }

            var response = await _mediator.Send(command, cancellationToken);

            return Ok(response);

            //return CreatedAtAction("GetAppById", "AppsQueryController", new { id = appId }, new { AppId = appId });
        }

        [HttpPost("{appId}/versions")]
        public async Task<IActionResult> SubmitNewVersion(
        Guid appId,
        [FromBody] SubmitNewVersionRequest request,
        CancellationToken cancellationToken)
        {
            var developerIdClaim = User.FindFirst("sub")?.Value
                ?? throw new UnauthorizedAccessException("Developer ID not found in token.");

            if (!Guid.TryParse(developerIdClaim, out var developerId))
                throw new InvalidOperationException("Invalid Developer ID format in token.");

            var command = new SubmitNewVersionCommand(
                developerId,
                appId,
                request.SubmissionId,
                request.VersionNumber,
                request.Changelog,
                request.MainAppFileName,
                request.MainAppChecksum,
                request.MainAppFileSize,
                request.Screenshots,
                request.Tags);

            var versionId = await _mediator.Send(command, cancellationToken);
            return Accepted(new { VersionId = versionId });
        }

        [HttpPut("{appId}/price")]
        public async Task<IActionResult> SetAppPrice(
        Guid appId,
        [FromBody] SetAppPriceRequest request,
        CancellationToken cancellationToken)
        {
            var developerIdClaim = User.FindFirst("sub")?.Value
                ?? throw new UnauthorizedAccessException("Developer ID not found in token.");

            if (!Guid.TryParse(developerIdClaim, out var developerId))
                throw new InvalidOperationException("Invalid Developer ID format in token.");

            var command = new SetAppPriceCommand(developerId, appId, request.Price);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpDelete("{appId}")]
        public async Task<IActionResult> DeleteApp(Guid appId, CancellationToken cancellationToken)
        {
            var developerIdClaim = User.FindFirst("sub")?.Value
                ?? throw new UnauthorizedAccessException("Developer ID not found in token.");

            if (!Guid.TryParse(developerIdClaim, out var developerId))
                throw new InvalidOperationException("Invalid Developer ID format in token.");

            var command = new DeleteAppCommand(developerId, appId);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpPost("{appId}/reviews")]
        [EnableRateLimiting("ReviewActionsPolicy")] // Apply rate limiting to this endpoint
        public async Task<IActionResult> SubmitReview(
       Guid appId,
       [FromBody] SubmitReviewRequest request,
       CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst("sub")?.Value
                ?? throw new UnauthorizedAccessException("User ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new InvalidOperationException("Invalid User ID format in token.");

            var command = new SubmitReviewCommand(userId, appId, request.Rating, request.Comment);
            try
            {
                await _mediator.Send(command, cancellationToken);
                return NoContent();
            }
            catch (DuplicateReviewException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }


    }

    public record SubmitReviewRequest(int Rating, string? Comment);
    public record SubmitNewVersionRequest(
    Guid SubmissionId,
    string VersionNumber,
    string? Changelog,
    string MainAppFileName,
    string MainAppChecksum,
    long MainAppFileSize,
    IReadOnlyList<ScreenshotInfo> Screenshots,
    IReadOnlyList<string> Tags);

    public record SetAppPriceRequest(decimal Price);
}
