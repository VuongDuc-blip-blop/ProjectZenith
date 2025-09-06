using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectZenith.Contracts.Commands.User;
using System.Security.Claims;

namespace ProjectZenith.Api.Write.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;
        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
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
        [HttpPost("register")]
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
                var registerResult = await _mediator.Send(command, cancellationToken);
                return CreatedAtAction(nameof(Register), new { userId = registerResult.UserId }, new
                {
                    registerResult.UserId,
                    registerResult.Email,
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
                var userEvent = await _mediator.Send(command, cancellationToken);
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

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var loginResult = await _mediator.Send(command, cancellationToken);
                return Ok(loginResult);
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var refreshTokenResult = await _mediator.Send(command, cancellationToken);
                return Ok(refreshTokenResult);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Errors = ex.Errors.Select(e => e.ErrorMessage) });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Logs out the authenticated user, invalidating their refresh token.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>No content on successful logout.</returns>
        /// <response code="204">Logout successful.</response>
        /// <response code="401">Unauthorized if JWT is invalid.</response>
        /// <response code="404">User not found.</response>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Logout([FromBody] LogoutCommand command, CancellationToken cancellationToken)
        {
            try
            {
                await _mediator.Send(command, cancellationToken);
                return NoContent();
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Errors = ex.Errors.Select(e => e.ErrorMessage) });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Revokes all active sessions for the authenticated user.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>No content on successful revocation.</returns>
        /// <response code="204">All sessions revoked successfully.</response>
        /// <response code="401">Unauthorized if JWT is invalid.</response>
        /// <response code="404">User not found.</response>
        [HttpPost("revoke-all-sessions")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevokeAllSessions(CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "Invalid user ID in token." });
            }

            var command = new RevokeAllSessionsCommand { UserId = userId };

            try
            {
                await _mediator.Send(command, cancellationToken);
                return NoContent();
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Errors = ex.Errors.Select(e => e.ErrorMessage) });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }



        /// <summary>
        /// Initiates a password reset by sending a reset link to the user's email.
        /// </summary>
        /// <param name="command">The password reset request command.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>No content on successful request.</returns>
        /// <response code="204">Reset email sent or no user found (silent).</response>
        /// <response code="400">Invalid email format.</response>
        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetCommand command, CancellationToken cancellationToken)
        {
            try
            {
                await _mediator.Send(command, cancellationToken);
                return NoContent();
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Errors = ex.Errors.Select(e => e.ErrorMessage) });
            }
        }

        /// <summary>
        /// Resets the user's password using a valid reset token.
        /// </summary>
        /// <param name="command">The password reset command.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>Success message on completion.</returns>
        /// <response code="200">Password reset successfully.</response>
        /// <response code="400">Invalid or expired token, or invalid password.</response>
        /// <response code="404">User not found.</response>
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken)
        {
            try
            {
                await _mediator.Send(command, cancellationToken);
                return Ok(new { Message = "Password reset successfully." });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Errors = ex.Errors.Select(e => e.ErrorMessage) });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }


        /// <summary>
        /// Updates the authenticated user's profile metadata.
        /// </summary>
        /// <param name="command">The profile update command.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>Updated profile data.</returns>
        /// <response code="200">Profile updated successfully.</response>
        /// <response code="400">Invalid input data.</response>
        /// <response code="401">Unauthorized if JWT is invalid.</response>
        /// <response code="404">User not found.</response>
        /// <response code="409">Concurrency conflict.</response>
        [HttpPatch("profile")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileCommand command, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "Invalid user ID in token." });
            }

            if (command.UserId != userId)
            {
                return Unauthorized(new { Message = "Cannot update another user's profile." });
            }

            try
            {
                await _mediator.Send(command, cancellationToken);
                return Ok(new
                {
                    command.UserId,
                    command.DisplayName,
                    command.Bio
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Errors = ex.Errors.Select(e => e.ErrorMessage) });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { Message = "Profile update failed due to concurrent modification." });
            }
        }

        /// <summary>
        /// Uploads a new avatar image for the authenticated user.
        /// </summary>
        /// <param name="file">The avatar image file (JPEG/PNG, max 2MB).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>Updated avatar URL.</returns>
        /// <response code="200">Avatar updated successfully.</response>
        /// <response code="400">Invalid file or file size.</response>
        /// <response code="401">Unauthorized if JWT is invalid.</response>
        /// <response code="404">User not found.</response>
        /// <response code="409">Concurrency conflict.</response>
        [HttpPost("avatar")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "Invalid user ID in token." });
            }
            var command = new UpdateUserAvatarCommand
            {
                UserId = userId,
                file = file
            };
            try
            {
                var updatedEvent = await _mediator.Send(command, cancellationToken);
                return Ok(new { updatedEvent.UserId, updatedEvent.Email, updatedEvent.AvatarUrl });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { Message = "Avatar update failed due to concurrent modification." });
            }
        }



    }
}
