
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectZenith.Contracts.Commands.App;
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

    }
    public record SubmitNewVersionRequest(
    Guid SubmissionId,
    string VersionNumber,
    string? Changelog,
    string MainAppFileName,
    string MainAppChecksum,
    long MainAppFileSize,
    IReadOnlyList<ScreenshotInfo> Screenshots,
    IReadOnlyList<string> Tags);
}
