namespace ProjectZenith.Contracts.DTOs.User
{
    /// <summary>
    /// DTO for user refresh token response.
    /// </summary>
    public record RefreshTokenResponseDTO
    {
        /// <summary>
        /// The access token for the user.
        /// </summary>
        public string AccessToken { get; init; } = string.Empty;

        /// <summary>
        /// The unique identifier for the new refresh token.
        /// </summary>
        public Guid NewRefreshTokenId { get; init; }

        /// <summary>
        /// The new refresh token for the user.
        /// </summary>
        public string NewRefreshToken { get; init; } = string.Empty;
    }
}
