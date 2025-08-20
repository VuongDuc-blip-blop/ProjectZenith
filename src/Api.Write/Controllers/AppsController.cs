
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

        public async Task<IActionResult> Submit(
            [FromForm] string name,
            [FromForm] string description,
            [FromForm] string category,
            [FromForm] string platform,
            [FromForm] decimal price,
            [FromForm] string versionNumber,
            [FromForm] string? changeLog,
            IFormFile appFile,
            CancellationToken cancellationToken)
        {
            var developerIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(developerIdString, out var developerId))
            {
                return Unauthorized("Invalid developer ID in token");
            }

            var command = new SubmitAppCommand
            {
                DeveloperId = developerId,
                Description = description,
                Category = category,
                Platform = platform,
                Price = price,
                VersionNumber = versionNumber,
                Changelog = changeLog,
                AppFile = appFile
            };

            var appId = await _mediator.Send(command, cancellationToken);

            return CreatedAtAction("GetAppById", "AppsQueryController", new { id = appId }, new { AppId = appId });
        }
    }
}
