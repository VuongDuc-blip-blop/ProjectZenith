using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Events
{
  /// <summary>
  /// Published when a user sucessfully registers in the system.
  /// </summary>
  /// <param name="UserId">The unique identifier of the user.</param>
  /// <param name="Email">The email address of the user.</param>
  /// <param name="Username">The optional username chosen by the user.</param>
  /// <param name="RegisteredAt">The date and time when the user registered.</param
  public record UserRegisteredEvent
  {
    [Required]
    public Guid UserId { get; init; }
    [Required]
    public string Email { get; init; }

    public string? Username { get; init; }
    [Required]
    [DataType(DataType.DateTime)]
    public DateTime RegisteredAt { get; init; }

  }
}
