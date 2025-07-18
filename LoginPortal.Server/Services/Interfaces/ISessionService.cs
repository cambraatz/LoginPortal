using LoginPortal.Server.Models;

namespace LoginPortal.Server.Services.Interfaces
{
    public interface ISessionService
    {
        Task<bool> AddOrUpdateSessionAsync(string username, string accessToken, string refreshToken, DateTime expiryTime, string? powerUnit, DateTime? mfstDate);
        Task<SessionModel?> GetSessionAsync(string username);
        Task<SessionModel?> GetSessionByManifestDetailsAsync(string username, string powerUnit, DateTime mfstDate);
        Task<bool> InvalidateSessionAsync(string username);
        Task<bool> InvalidateSessionByTokensAsync(string accessToken, string refreshToken); // For when tokens are revoked externally
        Task CleanupExpiredSessionsAsync(); // For background cleanup
    }
}
