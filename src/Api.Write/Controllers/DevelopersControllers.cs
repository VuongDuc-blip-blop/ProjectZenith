using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectZenith.Contracts.Commands.Developer;
using ProjectZenith.Contracts.Commands.User;
using System.Security.Claims;

namespace ProjectZenith.Api.Write.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DevelopersControllers : ControllerBase
    {
        private IMediator _mediator;
        public DevelopersControllers(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("request")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RequestDeveloperStatus(
            [FromBody] RequestDeveloperStatusCommand command,
            CancellationToken cancellationToken)
        {

            var userIdFromTokenString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdFromTokenString, out var userIdFromToken) || command.UserId != userIdFromToken)
            {
                return Forbid(); // Return 403 Forbidden if the user is trying to act on behalf of another.
            }


            var response = await _mediator.Send(command, cancellationToken);
            if (response != null)
            {
                // Auto-approval succeeded. Return the new tokens to the client.
                return Ok(response);
            }
            else
            {
                // Manual approval required. The request has been accepted for processing.
                return Accepted();
            }

        }

        [HttpPost("payout-onboarding-link")]
        public async Task<IActionResult> CreatePayoutOnboardingLink(
        [FromBody] CreateStripeConnectOnboardingLinkRequest request,
        CancellationToken cancellationToken)
        {
            var developerIdClaim = User.FindFirst("sub")?.Value
                ?? throw new UnauthorizedAccessException("Developer ID not found in token.");

            if (!Guid.TryParse(developerIdClaim, out var developerId))
                throw new InvalidOperationException("Invalid Developer ID format in token.");

            var command = new CreateStripeConnectOnboardingLinkCommand(
                developerId,
                request.ReturnUrl,
                request.RefreshUrl);

            var onboardingUrl = await _mediator.Send(command, cancellationToken);
            return Ok(new { OnboardingUrl = onboardingUrl });
        }

        [HttpPost("reconcile-payout-status")]
        public async Task<IActionResult> ReconcilePayoutStatus(CancellationToken cancellationToken)
        {
            var developerIdClaim = User.FindFirst("sub")?.Value
                ?? throw new UnauthorizedAccessException("Developer ID not found in token.");

            if (!Guid.TryParse(developerIdClaim, out var developerId))
                throw new InvalidOperationException("Invalid Developer ID format in token.");

            var command = new ReconcilePayoutStatusCommand(developerId);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }
    public record CreateStripeConnectOnboardingLinkRequest(string ReturnUrl, string RefreshUrl);
}
