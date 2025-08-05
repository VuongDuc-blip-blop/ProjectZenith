using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using ProjectZenith.Api.Write.Services.Commands.UserDomain;
using ProjectZenith.Contracts.Commands;

namespace ProjectZenith.Api.Write.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly RegisterUserCommandHandler _registerHandler;

        public UsersController(RegisterUserCommandHandler registerHandler)
        {
            _registerHandler = registerHandler;
        }

        /// <summary>
        /// Registers a new user in the system.
        /// </summary>
        /// <param name="command">The user registration data.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The created user's ID and email.</returns>
        /// <response code="201">User successfully registered.</response>
        /// <response code="400">Invalid input data.</response>
        /// <response code="409">Email already exists.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var userEvent = await _registerHandler.HandleAsync(command, cancellationToken);
                return CreatedAtAction(nameof(Register), new { userId = userEvent.UserId }, new
                {
                    userEvent.UserId,
                    userEvent.Email,
                });
            }
            catch (ValidationException ex) when (ex.Message.Contains("Email already exists"))
            {
                return Conflict(new { Message = ex.Message });
            }
        }
    }
}
