using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectZenith.Contracts.Models
{
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }
        [Required]
        public string RefreshTokenHash { get; set; } = null!;
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime RefreshTokenExpiresAt { get; set; }

        public string? DeviceInfo { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
