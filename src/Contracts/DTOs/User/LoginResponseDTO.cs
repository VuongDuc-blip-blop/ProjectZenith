namespace ProjectZenith.Contracts.DTOs.User
{
    /// <summary>
    /// DTO for user login response.
    /// </summary>
    public record LoginResponseDTO
    {
        /// <summary>
        /// The access token for the user.
        /// </summary>
        public string AccessToken { get; init; } = string.Empty;

        /// <summary>
        /// The unique identifier for the refresh token.
        /// </summary>
        public Guid RefreshTokenId { get; init; }

        /// <summary>
        /// The refresh token for the user.
        /// </summary>
        public string RefreshToken { get; init; } = string.Empty;
    }
}
