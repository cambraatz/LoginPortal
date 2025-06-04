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

        public SessionsController(IUserService userService, 
            ITokenService tokenService, 
            ILogger<SessionsController> logger, ICookieService cookieService)
        {
            _userService = userService;
            _tokenService = tokenService;
            //_connString = config.GetConnectionString("TCSWEB");
            _logger = logger;
            _cookieService = cookieService;
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
    }
}
