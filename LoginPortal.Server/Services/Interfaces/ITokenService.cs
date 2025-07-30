using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace LoginPortal.Server.Services.Interfaces
{
    public interface ITokenService
    {
        // generates tokens from provided username...
        Task<(string accessToken, string refreshToken)> GenerateToken(string username, long userId);

        // validates the access token, refreshes the tokens with refresh token when possible...
        Task<TokenValidation> ValidateTokens(string accessToken, string refreshToken, string userName, bool tryRefresh = true);
    }

    // stored in record for immutability + pattern-matching convenience...
    public sealed record TokenValidation
    (
        bool IsValid,
        string? Message = null,
        ClaimsPrincipal? Principal = null,
        string? AccessToken = null,
        string? RefreshToken = null
    );
}
