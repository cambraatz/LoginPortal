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
        public (string accessToken, string refreshToken) GenerateToken(string username)
        {
            var now = DateTimeOffset.UtcNow;

            /*Claim[] BaseClaims(string jti) => new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti,jti)
            };*/

            List<Claim> baseClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            //Console.WriteLine(_config["Jwt:Key"]);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            //Console.WriteLine($"token debug: key: {key}, creds: {creds}");

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

            // store session in database...
            _sessionService.AddOrUpdateSessionAsync(
                username,
                generatedAccess,
                generatedRefresh,
                refreshExpires,
                null,
                null
            ).Wait();

            return (generatedAccess, generatedRefresh);
        }

        /* Token Validation
         *  validates Jwt Security Token 
         */
        public TokenValidation ValidateTokens(string accessToken, string refreshToken, string username, bool tryRefresh = true)
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

            try
            {
                var principal = _handler.ValidateToken(accessToken, tokenParams, out var validated);

                // get refresh token's expirty for session update...
                var refreshJwt = _handler.ReadJwtToken(refreshToken);
                var refreshExpSession = DateTimeOffset.FromUnixTimeSeconds(refreshJwt.Payload.Expiration!.Value).UtcDateTime;

                // store session in database...
                _sessionService.AddOrUpdateSessionAsync(
                    username,
                    accessToken,
                    refreshToken,
                    refreshExpSession,
                    null,
                    null
                ).Wait();

                // token is still valid + not expiring soon...
                var exp = DateTimeOffset.FromUnixTimeSeconds(((JwtSecurityToken)validated).Payload.Expiration!.Value);
                if (exp - DateTimeOffset.UtcNow > TimeSpan.FromMinutes(5))
                {
                    //return new TokenValidation { IsValid = true, Principal = principal };
                    return new(true, Principal: principal);
                }

                if (!tryRefresh)
                {
                    //return new TokenValidation { IsValid = false, Message = "Access token is expiring soon, refreshing is disabled. Start new session to continue access." };
                    return new(false, "Access token is expiring soon, refreshing is disabled. Start new session to continue access.");

                }
                // token is expired, attempt to refresh...
                var refreshPrincipal = _handler.ValidateToken(refreshToken, tokenParams, out var valRef);
                var refreshExp = DateTimeOffset.FromUnixTimeSeconds(((JwtSecurityToken)valRef).Payload.Expiration!.Value);

                if (refreshExp <= DateTimeOffset.UtcNow)
                {
                    //return new TokenValidation { IsValid = false, Message = "Refresh token has expired, refresh access is denied. Start new session to continue access." };
                    return new(false, "Refresh token has expired, refresh access is denied. Start new session to continue access.");
                }
                else
                {
                    var (newAccess, newRefresh) = GenerateToken(username);
                    //return new TokenValidation { IsValid = true, Principal = principal, accessToken = newAccess, refreshToken = newRefresh };
                    return new(true, Principal: principal, AccessToken: newAccess, RefreshToken: newRefresh);
                }
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogError(ex, $"Token validation failed for user {username}: {ex.Message}");
                return new(false, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred during token validation for user {username}: {ex.Message}");
                return new(false, "An unexpected error occurred during token validation.");
            }
        }

        public (bool success, string message) AuthorizeRequest(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;

            // fetch username from cookies before interacting with tokens...
            var username = request.Cookies["username"];
            if (string.IsNullOrEmpty(username))
            {
                return (false, "Username is missing");
            }

            // fetch tokens from cookies to validate session...
            var accessToken = request.Cookies["access_token"];
            var refreshToken = request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                return (false, "Access token is missing");
            }

            // validate tokens + refresh if allowed...
            //var tokenService = new TokenService(_config);
            var result = ValidateTokens(accessToken, refreshToken, username);
            if (!result.IsValid)
            {
                _cookieService.DeleteCookies(context);
                return (false, "Invalid access token, authorization failed.");
            }

            // if non-null, replace tokens in cookies with fresh set...
            if (result.AccessToken != null && result.RefreshToken != null &&
                (result.AccessToken != accessToken || result.RefreshToken != refreshToken))
            {
                response.Cookies.Append("access_token", result.AccessToken, _cookieService.AccessOptions());
                response.Cookies.Append("refresh_token", result.RefreshToken, _cookieService.RefreshOptions());
            }

            return (true, "Token has been validated, authorization granted.");
        }
    }
}
