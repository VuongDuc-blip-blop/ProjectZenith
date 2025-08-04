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
    /// Represents the status of a payout.
    /// </summary>
    public enum PayoutStatus
    {
        Scheduled,
        Processing,
        Processed,
        Cancelled,
        Failed
    }

    /// <summary>
    /// Represents a payout to a developer for their earnings from app sales.
    /// </summary>
    public class Payout
    {
        /// <summary>
        /// Unique identifier for the payout.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The amount to be paid out to the developer.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// the currently status of the payout.
        /// </summary>
        public PayoutStatus Status { get; set; } = PayoutStatus.Scheduled;

        // --- Timestamps Tracking the Payout Lifecycle ---

        /// <summary>
        /// The timestamp when this payout was created and scheduled.
        /// </summary>
        public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The target date for when this payout should be processed. Can be null.
        /// </summary>
        public DateTime? ProcessAt { get; set; }

        /// <summary>
        /// The timestamp when the payout was confirmed as completed by the payment provider.
        /// </summary>
        public DateTime? CompletedAt { get; set; }


        // --- Payment Provider Details ---

        /// <summary>
        /// The name of the external payment provider (e.g., "Stripe"). Null until processing.
        /// </summary>
        [MaxLength(100)]
        public string? PaymentProvider { get; set; }

        /// <summary>
        /// The unique reference ID from the external payment provider. Null until processed.
        /// </summary>
        [MaxLength(256)]
        public string? PaymentId { get; set; }


        // --- Relationships ---
        /// <summary>
        /// The unique identifier of the developer this payout is for.
        /// </summary>
        public Guid DeveloperId { get; set; }

        /// <summary>
        /// navigation property to the developer this payout is for.
        /// </summary>
        [ForeignKey("DeveloperId")]
        public Developer Developer { get; set; } = null!;
    }
}
