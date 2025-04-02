using Microsoft.AspNetCore.Http;

namespace LoginPortal.Server.Services
{
    public class CookieService
    {
        public static CookieOptions RemoveOptions()
        {
            return new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(-1),
                HttpOnly = true,
                Secure = true,
                Domain = ".tcsservices.com",
                SameSite = SameSiteMode.None,
                Path = "/"
            };
        }
        public static CookieOptions AccessOptions()
        {
            return new CookieOptions
            {

                Expires = DateTime.UtcNow.AddMinutes(15),
                HttpOnly = true,
                Secure = true,
                Domain = ".tcsservices.com",
                SameSite = SameSiteMode.None,
                Path = "/"
            };
        }

        public static CookieOptions RefreshOptions()
        {
            return new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(1),
                HttpOnly = true,
                Secure = true,
                Domain = ".tcsservices.com",
                SameSite = SameSiteMode.None,
                Path = "/"
            };
        }

        public static void ExtendCookies(HttpContext context, int extensionMinutes)
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
                        Domain = ".tcsservices.com"
                    }
                );
            }
        }
    }
}
