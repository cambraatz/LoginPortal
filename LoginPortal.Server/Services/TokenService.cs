using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LoginPortal.Server.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;
        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public (string AccessToken, string RefreshToken) GenerateToken(string username, string companyDbName = null)
        {
            var accessClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (!string.IsNullOrEmpty(companyDbName))
            {
                accessClaims.Add(new Claim("company_db_name", companyDbName));
            }

            var refreshClaims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var accessToken = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: accessClaims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds);

            var refreshToken = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: refreshClaims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(accessToken), new JwtSecurityTokenHandler().WriteToken(refreshToken));
        }

        // Token validation result structure
        // Helper method to validate the access token
        public class TokenValidation
        {
            public bool IsValid { get; set; }
            public string? Message { get; set; }
            public ClaimsPrincipal? Principal { get; set; }
            public string? accessToken { get; set; }
            public string? refreshToken { get; set; }
        }

        public TokenValidation ValidateTokens(string accessToken, string refreshToken, string username)
        {
            TokenValidationParameters tokenParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
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
        }

        public (bool success, string message) AuthorizeRequest(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;

            var accessToken = request.Cookies["access_token"];
            var refreshToken = request.Cookies["refresh_token"];
            var username = request.Cookies["username"];
            //var company = Request.Cookies["company"];

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                return (false, "Access token is missing");
            }
            if (string.IsNullOrEmpty(username))
            {
                return (false, "Username is missing");
            }

            var tokenService = new TokenService(_configuration);
            var result = tokenService.ValidateTokens(accessToken, refreshToken, username);
            if (!result.IsValid)
            {
                return (false, "Invalid access token, authorization failed.");
            }

            accessToken = result.accessToken;
            refreshToken = result.refreshToken;

            if (accessToken != null && refreshToken != null)
            {
                response.Cookies.Append("access_token", accessToken, CookieService.AccessOptions());
                response.Cookies.Append("refresh_token", refreshToken, CookieService.RefreshOptions());
            }
            return (true, "Token has been validated, authorization granted.");
        }
    }

    /*public class RefreshRequest
    {
        public string Username { get; set; }
        public string RefreshToken { get; set; }
    }*/
}
