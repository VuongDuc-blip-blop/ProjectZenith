using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectZenith.Contracts.Enums;

namespace ProjectZenith.Contracts.Models
{
    /// <summary>
    /// Represents a developer profile in the system.
    /// </summary>
    public class Developer
    {
        /// <summary>
        /// The unique identifier for the developer.
        /// </summary>
        [Key]
        public Guid UserId { get; set; }

        /// <summary>
        /// The optional description of the developer.
        /// </summary>
        [StringLength(1000, ErrorMessage = "Description cannot be longer than 1000 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// The optional contact email for the developer.
        /// </summary>
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(255, ErrorMessage = "Contact Email cannot be longer than 255 characters.")]
        public string? ContactEmail { get; set; }

        /// <summary>
        /// The Stripe account ID associated with the developer for payment processing.
        /// </summary>
        [StringLength(255, ErrorMessage = "Stripe Account ID cannot be longer than 255 characters.")]
        public string StripeAccountId { get; set; } = string.Empty;

        /// <summary>
        /// The current payout status of the developer.
        /// </summary>
        [Required]
        [EnumDataType(typeof(DeveloperPayoutStatus), ErrorMessage = "Invalid payout status.")]
        public DeveloperPayoutStatus PayoutStatus { get; set; } = DeveloperPayoutStatus.NotOnboarded;

        /// <summary>
        /// The date and time when the developer profile was created.
        /// This is automatically set to the current UTC time when the profile is created.
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The date and time when the developer profile was last updated.
        /// This is automatically set to the current UTC time when the profile is updated.
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property to the user associated with this developer profile.
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// A list of applications created by the developer.
        /// </summary>
        public ICollection<App> Apps { get; set; } = new List<App>();
    }
}
