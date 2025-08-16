namespace ProjectZenith.Contracts.DTOs.User
{
    public record LoginResponseDTO
    {
        public string AccessToken { get; init; } = string.Empty;
        public Guid? RefreshTokenId { get; init; } = null;
        public string RefreshToken { get; init; } = string.Empty;
    }
}
