namespace ProjectZenith.Contracts.DTOs.User
{
    public record RegisterResponseDTO
    {
        public Guid? UserId { get; init; } = null;
        public string Email { get; init; } = string.Empty;
    }
}
