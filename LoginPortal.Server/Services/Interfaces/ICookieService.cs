using Microsoft.AspNetCore.Http;

namespace LoginPortal.Server.Services.Interfaces
{
    public interface ICookieService
    {
        CookieOptions RemoveOptions();
        CookieOptions AccessOptions();
        CookieOptions RefreshOptions();
        void ExtendCookies(HttpContext contex, int extensionMinutes);
        void DeleteCookies(HttpContext context);
    }
}
