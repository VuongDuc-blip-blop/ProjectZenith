namespace ProjectZenith.Contracts.DTOs.User
{
    /// <summary>
    /// DTO for user registration response.
    /// </summary>
    public record RegisterResponseDTO
    {
        /// <summary>
        /// The unique identifier for the user.
        /// </summary>
        public Guid? UserId { get; init; } = null;

        /// <summary>
        /// The email address of the user.
        /// </summary>
        public string Email { get; init; } = string.Empty;
    }
}
