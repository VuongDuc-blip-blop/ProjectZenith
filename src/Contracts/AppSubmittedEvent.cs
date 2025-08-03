namespace ProjectZenith.Contracts.Events;


/// <summary>
/// Represents the platform on which an application runs.
/// This will likely be moved to its own file later (e.g., Enums/Platform.cs).
/// </summary>
public enum Platform
{
  Android,
  Windows
}

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
  public Guid AppId { get; init; }
  public Guid DeveloperId { get; init; }
  public string AppName { get; init; }
  public string Description { get; init; }
  public string Category { get; init; }
  public Platform Platform { get; init; }
  public decimal Price { get; init; }
  public string Version { get; init; }
  public Guid FieldId { get; init; }
  public DateTime SubmittedAt { get; init; }
}
