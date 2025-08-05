using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectZenith.Contracts.Models
{
    /// <summary>
    /// Represents a user in the system.
    /// </summary>
    public class User
    {
        /// <summary>
        /// The unique identifier for the user.
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// The email address of the user.
        /// This is required and must be a valid email format.
        /// </summary>
        [Required]
        [EmailAddress]
        [StringLength(256, ErrorMessage = "Email cannot be longer than 256 characters.")]
        public string Email { get; set; } = null!;

        /// <summary>
        /// The optional username chosen by the user.
        /// This can be used for display purposes.
        /// </summary>
        [StringLength(100, ErrorMessage = "Username cannot be longer than 100 characters.")]
        public string? Username { get; set; }

        /// <summary>
        /// An optional biography or description of the user.
        /// </summary>

        [StringLength(500, ErrorMessage = "Bio cannot be longer than 500 characters.")]
        [DataType(DataType.MultilineText)]
        public string? Bio { get; set; }

        /// <summary>
        /// An optional URL to the user's avatar image.
        /// </summary>

        [Url(ErrorMessage = "Invalid URL format for AvatarUrl.")]
        [StringLength(2000, ErrorMessage = "Avatar URL cannot be longer than 200 characters.")]
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// The date and time when the user was created.
        /// This is automatically set to the current UTC time when the user is created.
        /// </summary>

        [Required]
        [DataType(DataType.DateTime)]

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The date and time when the user was last updated.
        /// </summary>

        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// A list of roles assigned to the user.
        /// </summary>
        public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();

        /// <summary>
        /// Navigation property to the user's credential.
        /// </summary>
        public Credential Credential { get; set; } = null!;

        /// <summary>
        /// Navigation property to the developer profile associated with this user.
        /// </summary>
        public Developer? Developer { get; set; }

        /// <summary>
        /// A list of reviews written by the user.
        /// </summary>
        public List<Review> Reviews { get; set; } = new List<Review>();

        /// <summary>
        /// A collection of all the abuse reports this user has FILED.
        /// This corresponds to the "Reporter" relationship.
        /// </summary>
        public ICollection<AbuseReport> FiledAbuseReports { get; set; } = new List<AbuseReport>();

        /// <summary>
        /// A collection of all the abuse reports FILED AGAINST this user.
        /// This corresponds to the "ReportedUser" relationship.
        /// </summary>
        public ICollection<AbuseReport> AbuseReportsAgainstUser { get; set; } = new List<AbuseReport>();

        /// <summary>
        /// A collection of all purchases made by the user.
        /// </summary>
        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();

    }
}
