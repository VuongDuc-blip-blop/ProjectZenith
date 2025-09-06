using MediatR;
using ProjectZenith.Contracts.DTOs.User;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands.User
{
    /// <summary>
    /// Command to register a new user.
    /// </summary>
    public record RegisterUserCommand : IRequest<RegisterResponseDTO>
    {
        /// <summary>
        /// The email of the user to register.
        /// </summary>
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
        public string Email { get; init; } = null!;

        /// <summary>
        /// The password of the user to register.
        /// </summary>
        [Required(ErrorMessage = "Password is required.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{8,}$",
            ErrorMessage = "Password must be at least 8 characters long, contain at least one uppercase letter, one lowercase letter, and one number.")]
        public string Password { get; init; } = null!;

        /// <summary>
        /// The username of the user to register.
        /// </summary>
        [StringLength(100, ErrorMessage = "Username cannot exceed 100 characters.")]
        public string? Username { get; init; }
    }
}
