using LoginPortal.Server.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LoginPortal.Server.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly JwtSecurityTokenHandler _handler = new();
        private readonly ILogger<TokenService> _logger;
        private readonly ICookieService _cookieService;
        private readonly ISessionService _sessionService;
        public TokenService(IConfiguration config, ILogger<TokenService> logger, ICookieService cookieService, ISessionService sessionService)
        {
            _config = config;
            _logger = logger;
            _cookieService = cookieService;
            _sessionService = sessionService;
        }

        /* Token Generation
         *  creates Jwt Security Tokens to maintain 
         *  authorization throughout the session...
         */
        public async Task<(string accessToken, string refreshToken)> GenerateToken(string username, long sessionId)
        {
            var now = DateTimeOffset.UtcNow;

            List<Claim> baseClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("sessionId", sessionId.ToString())
            };

            var configuredAudiences = _config["Jwt:Audience"];
            var audiences = configuredAudiences?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                .Select(audiences => audiences.Trim())
                                                .ToArray();
            if (audiences == null || audiences.Length == 0)
            {
                _logger.LogError("Jwt:Audience configuration is missing or empty.");
                audiences = Array.Empty<string>();
            }

            foreach (var aud in audiences!)
            {
                baseClaims.Add(new Claim(JwtRegisteredClaimNames.Aud, aud));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var access = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                //audience: _config["Jwt:Audience"],
                claims: baseClaims,
                expires: now.AddMinutes(15).UtcDateTime,
                signingCredentials: creds);

            var refreshExpires = now.AddDays(1).UtcDateTime;
            var refresh = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                //audience: _config["Jwt:Audience"],
                claims: baseClaims,
                expires: refreshExpires,
                signingCredentials: creds);

            var generatedAccess = _handler.WriteToken(access);
            var generatedRefresh = _handler.WriteToken(refresh);

            try
            {
                await _sessionService.AddOrUpdateSessionAsync(
                    sessionId,
                    username,
                    generatedAccess,
                    generatedRefresh,
                    refreshExpires,
                    null, // PowerUnit can be set later if needed
                    null  // MfstDate can be set later if needed
                );
                _logger.LogInformation("Session ID {SessionId} updated in DB with new tokens after generation.", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update session in DB after token generation for user {Username}.", username);
                throw; // Re-throw the exception to handle it upstream
            }

            return (generatedAccess, generatedRefresh);
        }

        /* Token Validation
         *  validates Jwt Security Token 
         */
        public async Task<TokenValidation> ValidateTokens(string accessToken, string refreshToken, string username, bool tryRefresh = true)
        {
            var tokenParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidAudiences = _config["Jwt:Audience"]?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()),
                //ValidAudience = _config["Jwt:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            ClaimsPrincipal? principal;
            SecurityToken? validatedAccess;
            bool accessExpired = false;

            try
            {
                principal = _handler.ValidateToken(accessToken, tokenParams, out validatedAccess);

                var currentSessionIdClaim = principal.FindFirst("sessionId");
                if (currentSessionIdClaim != null && long.TryParse(currentSessionIdClaim.Value, out long currentSessionId))
                {
                    await _sessionService.UpdateSessionLastActivityByIdAsync(currentSessionId); // Assuming this updates by username/token
                    _logger.LogDebug("Session ID {SessionId} last activity updated during access token validation.", currentSessionId);
                }
                else
                {
                    _logger.LogWarning("Session ID claim missing/invalid during access token validation for user {Username}. Last activity not updated by ID.", username);
                }

                // get refresh token's expirty for session update...
                var refreshJwt = _handler.ReadJwtToken(refreshToken);
                var refreshExpSession = DateTimeOffset.FromUnixTimeSeconds(refreshJwt.Payload.Expiration!.Value).UtcDateTime;

                // token is still valid + not expiring soon...
                var exp = DateTimeOffset.FromUnixTimeSeconds(((JwtSecurityToken)validatedAccess).Payload.Expiration!.Value);
                if (exp - DateTimeOffset.UtcNow > TimeSpan.FromMinutes(5))
                {
                    _logger.LogInformation("Access token is valid and not expiring soon for user: {Username}", username);
                    return new(true, Principal: principal);
                }
                else
                {
                    _logger.LogWarning("Access token is valid but expiring soon for user: {Username}. Attempting refresh.", username);
                    Console.WriteLine("Access token is valid but expiring soon for user: {Username}. Attempting refresh.", username);
                    accessExpired = true;
                }
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Access token expired for user: {Username}. Attempting refresh.", username);
                Console.WriteLine("Access token expired for user: {Username}. Attempting refresh.", username);
                accessExpired = true; // Mark for refresh
                principal = null; // Principal from expired token is not valid
            }
            catch (SecurityTokenValidationException ex)
            {
                _logger.LogError(ex, "Access token validation failed for user: {Username} (Reason: {Message})", username, ex.Message);
                Console.WriteLine("Access token validation failed for user: {Username} (Reason: {Message})", username, ex.Message);
                return new(false, "Invalid access token."); // Token invalid for other reasons, cannot refresh
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error validating access token for user: {Username}", username);
                return new(false, "An unexpected error occurred during access token validation.");
            }

            if (accessExpired && tryRefresh)
            {
                try
                {
                    var refreshTokenParams = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidIssuer = _config["Jwt:Issuer"],
                        ValidAudiences = _config["Jwt:Audience"]?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()),
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                    ClaimsPrincipal refreshPrincipal = _handler.ValidateToken(refreshToken, refreshTokenParams, out var validatedRefreshToken);
                    var refreshTokenExp = DateTimeOffset.FromUnixTimeSeconds(((JwtSecurityToken)validatedRefreshToken).Payload.Expiration!.Value);
                    if (refreshTokenExp <= DateTimeOffset.UtcNow)
                    {
                        _logger.LogWarning("Refresh token has expired for user: {Username}. Login required.", username);
                        return new(false, "Refresh token has expired. Please log in again.");
                    }

                    // extract session ID from refresh token...
                    var sessionIdClaim = refreshPrincipal.FindFirst("sessionId");
                    if (sessionIdClaim == null || !long.TryParse(sessionIdClaim.Value, out long sessionIdFromRefresh))
                    {
                        _logger.LogError("Refresh token is valid but missing or invalid 'sessionId' claim for user: {Username}. Cannot generate new tokens.", username);
                        return new(false, "Invalid refresh token: missing session ID.");
                    }

                    // refresh token is valid, generate new tokens...
                    _logger.LogInformation("Refresh token is valid for user: {Username}. Generating new tokens.", username);

                    // generate new token and UPDATE in session table...
                    var (newAccess, newRefresh) = await GenerateToken(username, sessionIdFromRefresh);

                    // re-validate the new access token to get the principal for the response...
                    var newPrincipal = _handler.ValidateToken(newAccess, tokenParams, out var _);

                    return new(true, Principal: newPrincipal, AccessToken: newAccess, RefreshToken: newRefresh);
                }
                catch (SecurityTokenExpiredException)
                {
                    _logger.LogWarning("Refresh token expired for user: {Username}. Login required.", username);
                    return new(false, "Refresh token has expired. Please log in again.");
                }
                catch (SecurityTokenValidationException ex)
                {
                    _logger.LogError(ex, "Refresh token validation failed for user: {Username} (Reason: {Message})", username, ex.Message);
                    return new(false, "Invalid refresh token. Please log in again.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error validating refresh token for user: {Username}", username);
                    return new(false, "An unexpected error occurred during refresh token validation.");
                }
            }

            else if (accessExpired && !tryRefresh)
            {
                _logger.LogInformation("Access token expired for user: {Username}, but refresh is disabled.", username);
                return new(false, "Access token is expired and refreshing is disabled. Start a new session.");
            }
            else
            {
                _logger.LogInformation("No token refresh performed or access token was already invalid for user: {Username}", username);
                return new(false, "Invalid or insufficient tokens to establish a session.");
            }
        }
    }
}
