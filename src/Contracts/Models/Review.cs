using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectZenith.Contracts.Models
{
    /// <summary>
    /// Represents a review for an application made by a user.
    /// </summary>
    public class Review
    {
        /// <summary>
        /// The unique identifier for the review.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The unique identifier for the user who posted the review.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The unique identifier for the application this review belongs to.
        /// </summary>
        public Guid AppId { get; set; }

        /// <summary>
        /// The rating given in the review, on a scale of 1 to 5.
        /// </summary>
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        /// <summary>
        /// The optional comment provided in the review.
        /// </summary>
        [StringLength(1000, ErrorMessage = "Comment cannot be longer than 1000 characters.")]
        public string? Comment { get; set; }

        /// <summary>
        /// Indicates whether the review has been edited.
        /// </summary>
        [Column(TypeName = "bit")]
        public bool IsEdited { get; set; } = false;

        /// <summary>
        /// The date and time when the review was posted.
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime PostedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The date and time when the review was last updated.
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; } = null;

        /// <summary>
        /// Navigation property to the user who posted the review.
        /// </summary>
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        /// <summary>
        /// Navigation property to the application this review belongs to.
        /// </summary>
        [ForeignKey("AppId")]
        public App App { get; set; } = null!;

        /// <summary>
        /// Navigation property to the abuse report associated with this review.
        /// </summary>
        public ICollection<AbuseReport> AbuseReports { get; set; } = new List<AbuseReport>();

    }
}
