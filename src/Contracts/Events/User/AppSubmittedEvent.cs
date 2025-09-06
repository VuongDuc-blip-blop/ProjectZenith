using System.ComponentModel.DataAnnotations;
using ProjectZenith.Contracts.Enums;

namespace ProjectZenith.Contracts.Events.User;




/// <summary>
/// Published when a developer successfully submits a new application for review.
/// This is an immutable record representing a fact that has occurred in the system.
/// </summary>
/// <param name="AppId">The unique identifier for the submitted application.</param>
/// <param name="DeveloperId">The ID of the developer who submitted the app.</param>
/// <param name="AppName">The initial name of the submitted application.</param>
/// <param name="Description">A brief description of the application.</param>
/// <param name="Category">The primary category of the application.</param>
/// <param name="Platform">The target platform for this app version.</param>
/// <param name="Price">The initial price of the application.</param>
/// <param name="Version">The initial version string (e.g., "1.0.0").</param>
/// <param name="FieldId">The ID of the field to which this application belongs.</param>
/// <param name="SubmittedAt">The UTC timestamp of the submission.</param>
public record AppSubmittedEvent
{
  [Required]
  public Guid AppId { get; init; }
  [Required]
  public Guid DeveloperId { get; init; }
  [Required]
  public string AppName { get; init; } = null!;
  [Required]
  public string Description { get; init; } = null!;
  [Required]
  public string Category { get; init; } = null!;
  [Required]
  [EnumDataType(typeof(Platform), ErrorMessage = "Invalid platform specified.")]
  public Platform Platform { get; init; }
  [Required]
  public decimal Price { get; init; }
  [Required]
  public string Version { get; init; } = null!;
  [Required]
  public DateTime SubmittedAt { get; init; }
}
