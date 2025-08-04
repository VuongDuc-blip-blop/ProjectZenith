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
    /// Represents the status of a transaction.
    /// </summary>
    public enum TransactionStatus
    {
        Pending,
        Completed,
        Failed
    }
    public class Transaction
    {
        /// <summary>
        /// The unique identifier for the transaction.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The unique identifier for the purchase associated with this transaction.
        /// </summary>
        public Guid PurchaseId { get; set; }

        /// <summary>
        /// The amount of the transaction.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// The payment provider used for the transaction (e.g., PayPal, Stripe).
        /// </summary>
        [Required]
        public string PaymentProvider { get; set; } = null!;

        /// <summary>
        /// The unique identifier for the payment transaction from the payment provider.
        /// </summary>
        [Required]
        public string PaymentId { get; set; } = null!;

        /// <summary>
        /// The status of the transaction.
        /// </summary>
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

        /// <summary>
        /// The date and time when the transaction was created.
        /// This is set to the current UTC time when the transaction is created.
        /// </summary>
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property to the purchase associated with this transaction.
        /// </summary>
        [ForeignKey("PurchaseId")]
        public Purchase Purchase { get; set; } = null!;
    }
}
