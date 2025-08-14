using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands
{
    public record RegisterUserCommand
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
        public string Email { get; init; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{8,}$",
            ErrorMessage = "Password must be at least 8 characters long, contain at least one uppercase letter, one lowercase letter, and one number.")]
        public string Password { get; init; } = null!;

        [StringLength(100, ErrorMessage = "Username cannot exceed 100 characters.")]
        public string? Username { get; init; }
    }
}
