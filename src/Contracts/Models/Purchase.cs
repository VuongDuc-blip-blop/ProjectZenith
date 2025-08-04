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
    /// Represents the status of a purchase.
    /// </summary>
    public enum PurchaseStatus
    {
        Pending,
        Completed,
        Refunded
    }

    /// <summary>
    /// Represents a purchase made by a user for an application.
    /// </summary>
    public class Purchase
    {
        /// <summary>
        /// The unique identifier for the purchase.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The unique identifier for the user who made the purchase.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The unique identifier for the application that was purchased.
        /// </summary>
        public Guid AppId { get; set; }

        /// <summary>
        /// The price of the purchase.
        /// This is stored as a decimal with a maximum of 18 digits and 2 decimal
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, 9999999999999999.99,
        ErrorMessage = "Price must be a positive value and cannot exceed 18 digits with 2 decimal places.")]
        public decimal Price { get; set; }

        /// <summary>
        /// The status of the purchase.
        /// </summary>
        public PurchaseStatus Status { get; set; } = PurchaseStatus.Pending;

        /// <summary>
        /// The date and time when the purchase was made.
        /// This is set to the current UTC time when the purchase is created.
        /// </summary>
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property to the user who made the purchase.
        /// </summary>
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        /// <summary>
        /// Navigation property to the application that was purchased.
        /// </summary>
        [ForeignKey("AppId")]
        public App App { get; set; } = null!;

        /// <summary>
        /// A list of transactions associated with the purchase.
        /// Each purchase can have multiple transactions, especially in cases of failed attempts or partial payments.
        /// </summary>
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    }
}
