using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectZenith.Contracts.Models
{
    public enum AbuseReportStatus
    {
        New,
        UnderReview,
        Resolved,
    }
    public class AbuseReport
    {
        /// <summary>
        /// The unique identifier for the abuse report.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// The reason for the abuse report.
        /// </summary>
        [Required, MaxLength(500)]
        public string Reason { get; private set; } = string.Empty;

        /// <summary>
        /// The status of the abuse report.
        /// </summary>
        public AbuseReportStatus Status { get; private set; }

        /// <summary>
        /// The date and time when the abuse report was created.
        /// This is set to the current UTC time when the report is created.
        /// </summary>
        public DateTime ReportedAt { get; private set; }

        // --- Relationships ---
        /// <summary>
        /// The unique identifier for the user who reported the abuse.
        /// </summary>
        public Guid ReporterId { get; private set; }

        /// <summary>
        /// Navigation property to the user who reported the abuse.
        /// </summary>
        [ForeignKey("ReporterId")]
        public User Reporter { get; private set; } = null!;

        // --- Report Targets (Nullable Foreign Keys) ---
        /// <summary>
        /// The unique identifier for the review this report is associated with.
        /// </summary>
        public Guid? ReviewId { get; private set; }

        /// <summary>
        /// Navigation property to the review this report is associated with.
        /// </summary>
        [ForeignKey("ReviewId")]
        public Review? Review { get; private set; }

        /// <summary>
        /// The unique identifier for the application this report is associated with.
        /// </summary>

        public Guid? AppId { get; private set; }

        /// <summary>
        /// Navigation property to the application this report is associated with.
        /// </summary>
        [ForeignKey("AppId")]
        public App? App { get; private set; }

        /// <summary>
        /// The unique identifier for the user this report is associated with.
        /// </summary>
        public Guid? UserId { get; private set; }

        /// <summary>
        /// Navigation property to the user this report is associated with.
        /// </summary>
        [ForeignKey("UserId")]
        public User? ReportedUser { get; private set; }

        // Private constructor for EF Core
        private AbuseReport() { }

        // Public "Factory" constructor that ENFORCES the business rule
        public AbuseReport(Guid reporterId, string reason, Guid? reviewId, Guid? appId, Guid? userId)
        {
            // This is the C# equivalent of your CHECK constraint
            if (!reviewId.HasValue && !appId.HasValue && !userId.HasValue)
            {
                throw new ArgumentException("An abuse report must have at least one target (ReviewId, AppId, or UserId).");
            }

            Id = Guid.NewGuid();
            ReporterId = reporterId;
            Reason = reason;
            ReviewId = reviewId;
            AppId = appId;
            UserId = userId;
            Status = AbuseReportStatus.New;
            ReportedAt = DateTime.UtcNow;
        }
    }
}
