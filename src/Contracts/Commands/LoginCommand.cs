using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Commands
{
    public record LoginCommand
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; init; }
        [Required(ErrorMessage = "Message is required")]
        public string Password { get; init; }

        public string? DeviceInfo { get; init; }
    }
}
