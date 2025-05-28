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
        public TokenService(IConfiguration config, ILogger<TokenService> logger)
        {
            _config = config;
            _logger = logger;
        }

        // Token validation result structure
        // Helper method to validate the access token
        /*public class TokenValidation
        {
            public bool IsValid { get; set; }
            public string? Message { get; set; }
            public ClaimsPrincipal? Principal { get; set; }
            public string? accessToken { get; set; }
            public string? refreshToken { get; set; }
        }*/
        /* single-use helper, doesnt stand to simplify much unless wide spread...
        private TokenValidation MintNewTokens(string userName, ClaimsPrincipal principal)
        {
            var (newAccess, newRefresh) = GenerateToken(userName);
            return new TokenValidation { IsValid = true, Principal = principal, accessToken = newAccess, refreshToken = newRefresh };
        }*/

        /* Token Generation
         *  creates Jwt Security Tokens to maintain 
         *  authorization throughout the session...
         */
        public (string accessToken, string refreshToken) GenerateToken(string username)
        {
            var now = DateTimeOffset.UtcNow;

            Claim[] BaseClaims(string jti) => new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti,jti)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var access = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: BaseClaims(Guid.NewGuid().ToString()),
                expires: now.AddMinutes(15).UtcDateTime,
                signingCredentials: creds);

            var refresh = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: BaseClaims(Guid.NewGuid().ToString()),
                expires: now.AddDays(1).UtcDateTime,
                signingCredentials: creds);

            return (_handler.WriteToken(access), _handler.WriteToken(refresh));
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
                ValidAudience = _config["Jwt:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = _handler.ValidateToken(accessToken, tokenParams, out var validated);

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
                //return new TokenValidation { IsValid = false, Message = ex.Message };
                return new(false, ex.Message);
            }
        }

        /*public TokenValidation ValidateTokens(string accessToken, string refreshToken, string username)
        {
            TokenValidationParameters tokenParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)),
                ValidateIssuer = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _config["Jwt:Audience"],
                ValidateLifetime = true,  // ensure token is not expired
                ClockSkew = TimeSpan.Zero
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(accessToken, tokenParams, out SecurityToken validatedToken);
                var jwtToken = validatedToken as JwtSecurityToken;
                var exp = jwtToken?.Payload.Exp ?? 0;
                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(exp);
                var currentTime = DateTimeOffset.UtcNow;



                // check if token is close to expiring
                if (expirationTime - currentTime < TimeSpan.FromMinutes(5))
                {
                    try
                    {
                        var refreshPrincipal = tokenHandler.ValidateToken(refreshToken, tokenParams, out SecurityToken validatedRefresh);
                        var refreshJwtToken = validatedRefresh as JwtSecurityToken;
                        var refreshExp = refreshJwtToken?.Payload.Exp ?? 0;
                        var refreshExpTime = DateTimeOffset.FromUnixTimeSeconds(exp);

                        if (refreshExpTime > currentTime)
                        {
                            var newTokens = GenerateToken(username);
                            return new TokenValidation { IsValid = true, Principal = principal, accessToken = newTokens.AccessToken, refreshToken = newTokens.RefreshToken };
                        }
                        else
                        {
                            return new TokenValidation { IsValid = false, Message = "Refresh token has expired." };
                        }
                    }
                    catch (Exception ex)
                    {
                        return new TokenValidation { IsValid = false, Message = $"Refresh token validation failed: {ex.Message}" };
                    }

                }

                return new TokenValidation { IsValid = true, Principal = principal };
            }
            catch (Exception ex)
            {
                return new TokenValidation { IsValid = false, Message = ex.Message };
            }
        }*/

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
                return (false, "Invalid access token, authorization failed.");
            }

            // valid tokens are returned, regardless of refresh status...
            accessToken = result.AccessToken;
            refreshToken = result.RefreshToken;

            // if non-null, replace tokens in cookies with fresh set...
            if (accessToken != null && refreshToken != null)
            {
                response.Cookies.Append("access_token", accessToken, CookieService.AccessOptions());
                response.Cookies.Append("refresh_token", refreshToken, CookieService.RefreshOptions());
            }

            return (true, "Token has been validated, authorization granted.");
        }
    }
}
