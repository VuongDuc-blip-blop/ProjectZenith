using ProjectZenith.Contracts.DTOs.User;
using ProjectZenith.Contracts.Models;

namespace ProjectZenith.Api.Write.Services.Security
{
    public interface ITokenService
    {
        Task<LoginResponseDTO> GenerateTokenAsync(User user, string? deviceInfo, CancellationToken cancellationToken);
    }
}
