using LoginPortal.Server.Models;

namespace LoginPortal.Server.Services.Interfaces
{
    public interface ISessionService
    {
        Task<SessionModel?> AddOrUpdateSessionAsync(long userId,string username,string accessToken,string refreshToken,DateTime expiryTime,string? powerUnit, string? mfstDate);
        Task<SessionModel?> AddSessionAsync(string username, string accessToken, string refreshToken, DateTime expiryTime, string? powerUnit, string? mfstDate);
        Task<bool> UpdateSessionLastActivityByIdAsync(long sessionId);
        Task<bool> UpdateSessionAsync(string username, string accessToken, string refreshToken, DateTime expiryTime, string? powerUnit, string? mfstDate);
        Task<bool> UpdateSessionLastActivityAsync(string username, string accessToken);
        Task<SessionModel?> GetSessionByIdAsync(long userId);
        Task<SessionModel?> GetSessionAsync(string username, string accessToken, string refreshToken);
        Task<SessionModel?> GetSessionByManifestDetailsAsync(string username, string powerUnit, string mfstDate, string accessToken, string refreshToken);
        Task<SessionModel?> GetConflictingSessionAsync(string currUsername, string powerUnit, string mfstDate, string accessToken, string refreshToken);
        Task<bool> DeleteSessionByIdAsync(long sessionId);
        Task<bool> InvalidateSessionAsync(string username, string accessToken, string refreshToken);
        Task<bool> InvalidateSessionByTokensAsync(string accessToken, string refreshToken);
        Task<bool> InvalidateSessionByDeliveryManifest(string username, string powerunit, string mfstdate);
        Task<bool> ResetSessionByDeliveryManifestAsync(string username, string powerunit, string mfstdate, string accessToken);
        Task CleanupExpiredSessionsAsync(TimeSpan idleTimeout); // For background cleanup
    }
}
