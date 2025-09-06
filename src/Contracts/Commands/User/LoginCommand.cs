using MediatR;
using ProjectZenith.Contracts.DTOs.User;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.User
{
    /// <summary>
    /// The command to log in a user.
    /// </summary>
    public record LoginCommand : IRequest<LoginResponseDTO>
    {
        /// <summary>
        /// The email of the user trying to log in.
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; init; }

        /// <summary>
        /// The password of the user trying to log in.
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; init; }

        /// <summary>
        /// Additional information about the device being used to log in.
        /// </summary>

        public string? DeviceInfo { get; init; }
    }
}
