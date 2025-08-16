using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events.User
{
    /// <summary>
    /// Published when a user posted a revire for an applicatiion
    /// </summary>
    /// <param name="ReviewId">The unique identifier for the review.</param>
    /// <param name="AppId">The ID of the the app being reviewed.</param>
    /// <param name="UserId">The ID of the user.</param>
    /// <param name="Rating">The rating point for the app.</param>
    /// <param name="Comment">The optional comment of the review.</param>
    /// <param name="PostedAt">The date and time when user posted the review.</param>
    public record ReviewPostedEvent
    {
        [Required]
        public Guid ReviewId { get; init; }
        [Required]
        public Guid AppId { get; init; }
        [Required]
        public Guid UserId { get; init; }
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; init; }

        public string? Comment { get; init; }
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime PostedAt { get; init; }
    }
}
