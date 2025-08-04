using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectZenith.Contracts.Models
{
    /// <summary>
    /// Represents a credential for a user in the system.
    /// A credential typically includes a password hash and the user it belongs to.
    /// </summary>
    public class Credential
    {
        /// <summary>
        /// The unique identifier of the user this credential belongs to.
        /// </summary>
        [Key]
        public Guid UserId { get; set; }

        /// <summary>
        /// The password hash for the user.
        /// This is used for authentication purposes and should be securely stored.
        /// </summary>
        [Required]
        [StringLength(256, ErrorMessage = "Password hash cannot be longer than 256 characters.")]
        public string PasswordHash { get; set; }

        /// <summary>
        /// The date and time when the credential was created.
        /// This is automatically set to the current UTC time when the credential is created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
    }
}
