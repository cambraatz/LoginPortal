/*/////////////////////////////////////////////////////////////////////////////
 
Author: Cameron Braatz
Date: 5/21/2024
Update: --/--/----

*//////////////////////////////////////////////////////////////////////////////

using LoginPortal.Server.Models;
using LoginPortal.Server.Services;
using LoginPortal.Server.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

// token initialization...
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json.Linq;

using System.Web;
using System.Net;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Reflection;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http.HttpResults;

/*/////////////////////////////////////////////////////////////////////////////
 
Registration Controller API Functions

API endpoint functions that handle standard user credential verification, 

API Endpoints (...api/Registration/*):
    Login: check database for matching username/password combo, divert admins
    

*//////////////////////////////////////////////////////////////////////////////

namespace LoginPortal.Server.Controllers
{
    [ApiController]
    [Route("v1/sessions")]
    public class SessionsController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        //private readonly string _connString;
        private readonly ILogger<SessionsController> _logger;
        private readonly ICookieService _cookieService;
        private readonly ISessionService _sessionService;

        public SessionsController(IUserService userService, 
            ITokenService tokenService, ISessionService sessionService,
            ILogger<SessionsController> logger, ICookieService cookieService)
        {
            _userService = userService;
            _tokenService = tokenService;
            //_connString = config.GetConnectionString("TCSWEB");
            _logger = logger;
            _cookieService = cookieService;
            _sessionService = sessionService;
        }

        private void UpdateSessionCookies(HttpResponse response, User user, string accessToken, string refreshToken)
        {
            Response.Cookies.Append("access_token", accessToken, _cookieService.AccessOptions());
            Response.Cookies.Append("refresh_token", refreshToken, _cookieService.RefreshOptions());
            Response.Cookies.Append("username", user.Username!, _cookieService.AccessOptions());
            Response.Cookies.Append("company", user.ActiveCompany!, _cookieService.AccessOptions());
            Console.WriteLine("Stored User information in cookies...");
        }

        // fetch full user credentials to auto-login when return cookie is found...

        /*/////////////////////////////////////////////////////////////////////////////
 
        Login(username, password)

        Queries the USERS database table for any users matching the provided username 
        and password combination provided by the user. Successful authorization
        generates the access tokens and caches tokens and critical user data in cookies 
        for use later.

        On success, the user credentials (including any companies/modules they have 
        access to) are passed back to the client for dynamic app navigation based on
        predefined permissions.

        *//////////////////////////////////////////////////////////////////////////////

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] loginCredentials creds)
        {
            User? user = await _userService.AuthenticateAsync(creds.USERNAME!, creds.PASSWORD!);
            if (user == null) 
            {
                _logger.LogError("Error generating tokens for user {Username}", creds.USERNAME);
                Console.WriteLine($"Error generating tokens for user {creds.USERNAME}");
                return Unauthorized(new { message = "Invalid user credentials, contact system administrator." });
            }

            if (user.Companies == null || user.Companies!.Count == 0 || user.Modules == null || user.Modules!.Count == 0)
            {
                _logger.LogError("Error generating tokens for user {Username}", user.Username);
                Console.WriteLine($"Error generating tokens for user {user.Username}");
                return BadRequest(new { message = "No results found for company and/or module permissions for the current user, contact system administrator." });
            }

            string access, refresh;
            try 
            {
                (access, refresh) = _tokenService.GenerateToken(user.Username!);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tokens for user {Username}", user.Username);
                Console.WriteLine(ex);
                return StatusCode(500, "Internal error generating authentication tokens.");
            }


            /*Response.Cookies.Append("access_token", access, CookieService.AccessOptions());
            Response.Cookies.Append("refresh_token", refresh, CookieService.RefreshOptions());
            Response.Cookies.Append("username", user.Username!, CookieService.AccessOptions());
            Response.Cookies.Append("company", user.ActiveCompany!, CookieService.AccessOptions());*/

            UpdateSessionCookies(Response, user, access, refresh);

            return Ok(new { user });
        }

        /*/////////////////////////////////////////////////////////////////////////////
 
        Logout()

        Iteratively 'expire' all active cookies, leaving a clean slate for new users.
        Successful logout return Ok status to frontent.

        *//////////////////////////////////////////////////////////////////////////////
        [HttpPost]
        [Route("logout")]
        public IActionResult Logout()
        {
            foreach (var cookie in Request.Cookies)
            {
                Response.Cookies.Append(cookie.Key, "", _cookieService.RemoveOptions());
            }
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpPost]
        [Route("credentials")]
        public async Task<IActionResult> Credentials()
        {
            // ensure return flag exists in cookies and validate its status...
            string? returnString;
            bool validReturn;
            if (!Request.Cookies.TryGetValue("return", out returnString) || !bool.TryParse(returnString, out validReturn) || !validReturn)
            {
                return BadRequest(new { message = "Valid return cookie not found." });
            }
            Response.Cookies.Append("return", "", _cookieService.RemoveOptions());

            // ensure username exists in cookies and is non-null...
            string? username;
            if(!Request.Cookies.TryGetValue("username", out username) || string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized(new { message = "Username was not found in cookies." });
            }

            // ask the service for the user...
            var user = await _userService.GetByUsernameAsync(username);
            if (user == null)
            {
                return Unauthorized(new { message = "User does not exist in company records." });
            }

            if (user.Companies!.Count == 0 || user.Modules!.Count == 0)
            {
                return Forbid("Valid user has no assigned company/module permissions, contact system administrator.");
            }

            string access, refresh;
            (access, refresh) = _tokenService.GenerateToken(user.Username!);

            UpdateSessionCookies(Response, user, access, refresh);

            return Ok(new { user });
        }

        [HttpPost]
        [Route("check-manifest-access")]
        [Authorize]
        public async Task<IActionResult> CheckManifestAccess([FromBody] ManifestAccessRequest request)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("Attempt to access check-manifest-access without username claim.");
                return Unauthorized("User identity not found.");
            }

            // The check `request.MfstDate == default(DateTime)` will now also catch cases
            // where `MfstDateString` couldn't be parsed into a valid date.
            if (string.IsNullOrEmpty(request.PowerUnit) || request.MfstDate == default(DateTime))
            {
                _logger.LogWarning("CheckManifestAccess: Power Unit or Manifest Date (or format issue) missing/invalid for user {Username}. Received PowerUnit: '{PowerUnit}', MfstDateString: '{MfstDateString}'",
                                   username, request.PowerUnit, request.MfstDateString);
                return BadRequest("Power Unit and Manifest Date are required and must be in 'MMDDYYYY' format.");
            }

            _logger.LogInformation("CheckManifestAccess: Checking for SSO conflicts for user {Username} on PowerUnit {PowerUnit} and ManifestDate {MfstDate}",
                                   username, request.PowerUnit, request.MfstDate.ToShortDateString());
            var conflictingSession = await _sessionService.GetConflictingSessionAsync(username, request.PowerUnit, request.MfstDate.Date);

            if (conflictingSession != null)
            {
                _logger.LogWarning("SSO conflict detected! User {ConflictingUser} is already accessing PowerUnit {PowerUnit} on ManifestDate {MfstDate}. Invalidating their session.",
                                   conflictingSession.Username, request.PowerUnit, request.MfstDate.ToShortDateString());

                await _sessionService.InvalidateSessionAsync(conflictingSession.Username);

                return Forbid("Another user is currently accessing this Power Unit and Manifest Date. Their session has been terminated to allow your access. Please try again or refresh their page.");
            }

            var currentAccessToken = Request.Cookies["access_token"];
            var currentRefreshToken = Request.Cookies["refresh_token"];

            if (string.IsNullOrEmpty(currentAccessToken) || string.IsNullOrEmpty(currentRefreshToken))
            {
                _logger.LogWarning("CheckManifestAccess: Missing access or refresh token for user {Username} during manifest session update.", username);
                return Unauthorized("Session tokens missing. Please log in again.");
            }

            DateTime refreshExpiryTime = DateTime.UtcNow.AddDays(1);
            try
            {
                var refreshJwtToken = new JwtSecurityTokenHandler().ReadJwtToken(currentRefreshToken);
                if (refreshJwtToken.Payload.Expiration.HasValue)
                {
                    refreshExpiryTime = DateTimeOffset.FromUnixTimeSeconds(refreshJwtToken.Payload.Expiration.Value).UtcDateTime;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckManifestAccess: Could not parse refresh token expiry for user {Username}. Using default expiry.", username);
            }

            var success = await _sessionService.AddOrUpdateSessionAsync(
                username,
                currentAccessToken,
                currentRefreshToken,
                refreshExpiryTime,
                request.PowerUnit,
                request.MfstDate.Date
            );

            if (!success)
            {
                _logger.LogError("CheckManifestAccess: Failed to update session details for user {Username} with PowerUnit {PowerUnit} and ManifestDate {MfstDate}.",
                                 username, request.PowerUnit, request.MfstDate.ToShortDateString());
                return StatusCode(500, "Failed to update session with manifest details.");
            }

            _logger.LogInformation("CheckManifestAccess: Manifest access granted and session updated for user {Username} to PowerUnit {PowerUnit} and ManifestDate {MfstDate}.",
                                   username, request.PowerUnit, request.MfstDate.ToShortDateString());

            return Ok("Manifest access granted and session updated.");
        }
    }
}
