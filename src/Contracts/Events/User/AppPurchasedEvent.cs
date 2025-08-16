using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events.User
{
    /// <summary>
    /// Represents an event that is published when a user purchases an application.
    /// </summary>
    public record AppPurchasedEvent
    {
        /// <summary>
        /// Unique identifier for the purchase.
        /// </summary>
        [Required]
        public Guid PurchaseId { get; init; }

        /// <summary>
        /// The ID of the user who made the purchase.
        /// </summary>
        [Required]
        public Guid UserId { get; init; }

        /// <summary>
        /// The ID of the application that was purchased.
        /// </summary>
        [Required]
        public Guid AppId { get; init; }

        /// <summary>
        /// The price at which the application was purchased.
        /// </summary>
        [Required]
        public decimal Price { get; init; }

        /// <summary>
        /// The date and time when the purchase was made.
        /// </summary>
        [Required]
        public DateTime PurchaseDate { get; init; }
    }
}
