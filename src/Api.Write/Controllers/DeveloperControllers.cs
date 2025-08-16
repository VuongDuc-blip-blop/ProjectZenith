using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectZenith.Contracts.Commands.User;
using System.Security.Claims;

namespace ProjectZenith.Api.Write.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DeveloperControllers : ControllerBase
    {
        private IMediator _mediator;
        public DeveloperControllers(IMediator mediator)
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


            await _mediator.Send(command, cancellationToken);

            return NoContent();
        }
    }
}
