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
        private readonly VerifyEmailCommandHandler _verifyEmailHandler;

        public UsersController(RegisterUserCommandHandler registerHandler, VerifyEmailCommandHandler verifyEmailHandler)
        {
            _registerHandler = registerHandler;
            _verifyEmailHandler = verifyEmailHandler;
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
            catch (ValidationException ex) when (ex.Message.Contains("Email already exists") || ex.Message.Contains("Username already exists"))
            {
                return Conflict(new { Message = ex.Message });
            }
        }


        [HttpPost("verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailCommand command, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userEvent = await _verifyEmailHandler.Handle(command, cancellationToken);
                return Ok(new
                {
                    userEvent.UserId,
                    userEvent.Email,
                    userEvent.VerifiedAt
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Invalid or expired token"))
            {
                throw;
                return StatusCode(StatusCodes.Status410Gone, new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception (not shown here for brevity)
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = ex.Message });
            }

        }
    }
}
