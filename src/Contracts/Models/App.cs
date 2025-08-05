using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectZenith.Contracts.Events;

namespace ProjectZenith.Contracts.Models
{
    /// <summary>
    /// Represent the status of an application.
    /// </summary>
    public enum Status
    {
        Draft,
        Pending,
        Published,
        Rejected,
        Banned
    }

    /// <summary>
    /// Represents an application in the system.
    /// </summary>
    public class App
    {
        /// <summary>
        /// The unique identifier for the application.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// The unique identifier for the developer who created the application.
        /// </summary>
        [Required]
        public Guid DeveloperId { get; set; }
        [Required]
        [StringLength(100, ErrorMessage = "Applicatioin's name cannot be longer than 100 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The description of the application.
        /// </summary>
        [Required]
        [StringLength(1000, ErrorMessage = "Application's description cannot be longer than 1000 characters.")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The category of the application.
        /// </summary>
        [Required]
        [StringLength(50, ErrorMessage = "Application's category cannot be longer than 50 characters.")]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// The platform on which the application is available.
        /// </summary>
        [Required]
        public Platform Platform { get; set; }

        /// <summary>
        /// The price of the application.
        /// </summary>
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a non-negative value.")]
        public decimal Price { get; set; }

        /// <summary>
        /// The application's status.
        /// </summary>
        [Required]
        public Status Status { get; set; } = Status.Draft;

        /// <summary>
        /// The date and time when the application was created.
        /// This is automatically set to the current UTC time when the application is created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The date and time when the application was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// The app's developer.
        /// </summary>
        public Developer Developer { get; set; } = null!;

        /// <summary>
        /// A list of versions associated with the application.
        /// </summary>
        public ICollection<AppVersion> Versions { get; set; } = new List<AppVersion>();

        /// <summary>
        /// A list of abuse reports associated with the application.
        /// </summary>
        public ICollection<AbuseReport> AbuseReports { get; set; } = new List<AbuseReport>();

        /// <summary>
        /// A list of reviews associated with the application.
        /// </summary>
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
