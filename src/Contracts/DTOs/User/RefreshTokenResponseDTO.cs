namespace ProjectZenith.Contracts.DTOs.User
{
    public class RefreshTokenResponseDTO
    {
        public string AccessToken { get; init; } = string.Empty;
        public Guid NewRefreshTokenId { get; init; }
        public string NewRefreshToken { get; init; } = string.Empty;
    }
}
