using ProjectZenith.Contracts.Enums;
using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.DTOs
{
    /// <summary>
    /// Represents a summary of an application for browsing or seach result.
    /// This DTO is used to transfer basic application data between layers of the application.
    /// </summary>
    /// <param name="AppId">The unique identifier of the application.</param>
    /// <param name="Name">The name of the application.</param>
    /// <param name="Description">A brief description of the application.</param>
    /// <param name="Category">The primary category of the application.</param>
    /// <param name="Platform">The target platform for the application.</param>
    /// <param name="Price">The price of the application.</param>
    /// <param name="AverageRating">The average rating of the application, if available.</param>
    public record AppSummaryDto
    {
        [Required]
        public Guid AppId { get; init; }
        [Required]
        public string Name { get; init; }
        [Required]
        public string Description { get; init; }
        [Required]
        public string Category { get; init; }
        [Required]
        public Platform Platform { get; init; }
        [Required]
        public decimal Price { get; init; }
        public double? AverageRating { get; init; }
    }
}
