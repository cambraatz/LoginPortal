using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using LoginPortal.Server.Services.Interfaces;

namespace LoginPortal.Server.Services
{
    public class CookieService : ICookieService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<CookieService> _logger;

        public CookieService(IWebHostEnvironment env, IConfiguration config, ILogger<CookieService> logger)
        {
            _env = env;
            _config = config;
            _logger = logger;
        }

        private string? GetCookieDomain()
        {
            if (_env.IsDevelopment())
            {
                // let domain be set to current origins domain (likely localhost)
                _logger.LogInformation("CookieService: Running in Development environment, setting cookie domain to null (auto-localhost).");
                Console.WriteLine("CookieService: Running in Development environment, setting cookie domain to null (auto-localhost).");
                return null;
            }
            // explicitly set origin domain to deployment URL
            _logger.LogInformation("CookieService: Running in non-Development environment, setting cookie domain to .tcsservices.com.");
            return ".tcsservices.com";
        }
        public CookieOptions RemoveOptions()
        {
            return new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(-1),
                HttpOnly = true,
                Secure = true,
                Domain = GetCookieDomain(),
                SameSite = SameSiteMode.None,
                Path = "/"
            };
        }
        public CookieOptions AccessOptions()
        {
            return new CookieOptions
            {

                Expires = DateTime.UtcNow.AddMinutes(15),
                HttpOnly = true,
                Secure = true,
                Domain = GetCookieDomain(),
                SameSite = SameSiteMode.None,
                Path = "/"
            };
        }

        public CookieOptions RefreshOptions()
        {
            return new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(1),
                HttpOnly = true,
                Secure = true,
                Domain = GetCookieDomain(),
                SameSite = SameSiteMode.None,
                Path = "/"
            };
        }

        public void ExtendCookies(HttpContext context, int extensionMinutes)
        {
            var response = context.Response;
            var request = context.Request;

            foreach (var cookie in request.Cookies)
            {
                response.Cookies.Append(
                    cookie.Key,
                    cookie.Value,
                    new CookieOptions
                    {
                        Expires = DateTime.UtcNow.AddMinutes(extensionMinutes),
                        HttpOnly = true,  // Keeps it secure from JavaScript access
                        Secure = true,    // Ensures cookies are only sent over HTTPS
                        SameSite = SameSiteMode.None, // Allows access across subdomains
                        Domain = GetCookieDomain()
                    }
                );
            }
        }
    }
}
