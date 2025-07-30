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

            // Add the session record in the database
            SessionModel? initialSession = await _sessionService.AddOrUpdateSessionAsync(
                0, // pass zero to add new session...
                user.Username!,
                "access_placeholder", // placeholders to be updated...
                "refresh_placeholder",
                DateTime.UtcNow.AddDays(1),
                null,
                null
            );

            if (initialSession == null || initialSession.Id == 0)
            {
                _logger.LogError("Login: Failed to create initial session record for user {Username}.", user.Username);
                return StatusCode(500, "Failed to initialize session in the database.");
            }

            string access, refresh;
            try 
            {
                (access, refresh) = await _tokenService.GenerateToken(user.Username!, initialSession.Id);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tokens for user {Username}", user.Username);
                await _sessionService.DeleteSessionByIdAsync(initialSession.Id);
                return StatusCode(500, "Internal error generating authentication tokens.");
            }

            DateTime refreshExpiryTime = DateTime.UtcNow.AddDays(1);
            try
            {
                var refreshJwtToken = new JwtSecurityTokenHandler().ReadJwtToken(refresh);
                if (refreshJwtToken.Payload.Expiration.HasValue)
                {
                    refreshExpiryTime = DateTimeOffset.FromUnixTimeSeconds(refreshJwtToken.Payload.Expiration.Value).UtcDateTime;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DevLogin: Could not parse refresh token expiry for user {Username}. Using default expiry.", creds.USERNAME);
            }

            UpdateSessionCookies(Response, user, access, refresh);

            _logger.LogInformation("User {Username} logged in successfully. Session ID: {SessionId}", user.Username, initialSession.Id);
            return Ok(new { user });
        }

        /*/////////////////////////////////////////////////////////////////////////////
 
        Logout()

        Iteratively 'expire' all active cookies, leaving a clean slate for new users.
        Successful logout return Ok status to frontent.

        *//////////////////////////////////////////////////////////////////////////////
        [HttpPost]
        [Route("logout")]
        public async Task<IActionResult> Logout()
        {
            string? username = Request.Cookies["username"];
            string? accessToken = Request.Cookies["access_token"];
            string? refreshToken = Request.Cookies["refresh_token"];
            _logger.LogWarning($"Attempting to logout with collected cookies; username: {username}, access: {accessToken} and refresh: {refreshToken}");
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
            {
                SessionModel? session = await _sessionService.GetSessionAsync(username!, accessToken!, refreshToken!);
                if (session != null)
                {
                    _logger.LogWarning($"Invalidating sesssion by tokens {accessToken} and {refreshToken}");
                    bool sessionCleared = await _sessionService.DeleteSessionByIdAsync(session.Id);
                }
            }

            /*SessionModel? session = await _sessionService.GetSessionByIdAsync(sessionId);
            if (session != null)
            {
                _logger.LogWarning($"Invalidating sesssion by ID {sessionId}");
                bool sessionCleared = await _sessionService.DeleteSessionByIdAsync(sessionId);
            }*/


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

            string? accessToken = Request.Cookies["access_token"];
            string? refreshToken = Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Credentials: Access or refresh token missing from cookies for user {Username}.", username);
                return Unauthorized(new { message = "Authentication tokens missing. Please log in again." });
            }
            // validate and refresh tokens as necessary...
            _logger.LogInformation("Credentials: Attempting to validate and refresh tokens for user {Username}.", username);
            var tokenValidationResult = await _tokenService.ValidateTokens(accessToken, refreshToken, username, tryRefresh: true);
            if (!tokenValidationResult.IsValid)
            {
                _logger.LogWarning("Credentials: Token validation/refresh failed for user {Username}. Reason: {Message}", username, tokenValidationResult.Message);
                _cookieService.DeleteCookies(HttpContext); // Clear all cookies on failure
                return Unauthorized(new { message = tokenValidationResult.Message });
            }

            // tokens are valid and updated in the DB session if refreshed...

            // ask the service for the user...
            var user = await _userService.GetByUsernameAsync(username);
            if (user == null)
            {
                _logger.LogError("Credentials: User details not found for username {Username} after successful token validation.", username);
                _cookieService.DeleteCookies(HttpContext); // Clear cookies if user profile is missing
                return Unauthorized(new { message = "User does not exist in company records." });
            }

            if (user.Companies == null || user.Companies.Count == 0 || user.Modules == null || user.Modules.Count == 0)
            {
                _logger.LogWarning("Credentials: User {Username} has no assigned company/module permissions.", username);
                _cookieService.DeleteCookies(HttpContext); // Clear cookies if permissions are missing
                return Forbid("Valid user has no assigned company/module permissions, contact system administrator.");
            }

            // update cookies with new tokens...
            if (tokenValidationResult.AccessToken != null && tokenValidationResult.RefreshToken != null &&
                (tokenValidationResult.AccessToken != accessToken || tokenValidationResult.RefreshToken != refreshToken))
            {
                _logger.LogInformation("Credentials: Updating client cookies with new access and refresh tokens for user {Username}.", username);
                Response.Cookies.Append("access_token", tokenValidationResult.AccessToken, _cookieService.AccessOptions());
                Response.Cookies.Append("refresh_token", tokenValidationResult.RefreshToken, _cookieService.RefreshOptions());
            }
            else
            {
                _logger.LogWarning("Credentials: Tokens were not updated for user {Username} after validation.", username);
            }
            //string access, refresh;
            //(access, refresh) = _tokenService.GenerateToken(user.Username!);

            //UpdateSessionCookies(Response, user, access, refresh);
            Response.Cookies.Append("username", user.Username!, _cookieService.AccessOptions());
            Response.Cookies.Append("company", user.ActiveCompany!, _cookieService.AccessOptions());
            Response.Cookies.Append("company_mapping", Newtonsoft.Json.JsonConvert.SerializeObject(user.Companies), _cookieService.AccessOptions());
            Response.Cookies.Append("module_mapping", Newtonsoft.Json.JsonConvert.SerializeObject(user.Modules), _cookieService.AccessOptions());

            _logger.LogInformation("Credentials: User {Username} successfully re-authenticated and session refreshed.", username);
            return Ok(new { user });
        }
    }
}
