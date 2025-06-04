using LoginPortal.Server.Models;
using LoginPortal.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoginPortal.Server.Controllers
{
    [ApiController]
    [Route("v1/drivers")]
    public class DriversController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<DriversController> _logger;
        private readonly ICookieService _cookieService;
        private readonly string? _connString;

        public DriversController(IConfiguration config,
            IUserService userService, 
            ITokenService tokenService, 
            ILogger<DriversController> logger, 
            ICookieService cookieService)
        {
            _config = config;
            _userService = userService;
            _tokenService = tokenService;
            _logger = logger;
            _cookieService = cookieService;
            _connString = config.GetConnectionString("TCSWEB");
        }

        [HttpGet]
        [Route("{username}")]
        public async Task<IActionResult> GetDriver(string username)
        {
            User? user = await _userService.GetByUsernameAsync(username);
            if (user == null)
            {
                return NotFound(new { message = "Driver not found." });
            }

            return Ok(new { user });
        }

        [HttpPut]
        [Route("{username}")]
        public async Task<IActionResult> UpdateDriver(string username, [FromBody] driverCredentials user)
        {
            if (user == null || user.USERNAME == null || user.PASSWORD == null || user.POWERUNIT == null)
            {
                return BadRequest(new { message = "Invalid user credentials, request denied." });
            }
            else if (user.USERNAME == null || user.PASSWORD == null || user.POWERUNIT == null)
            {
                return BadRequest(new { message = "Incomplete user credentials provided, request denied." });
            }

            int success = await _userService.UpdateUserAsync(user.USERNAME!, user.PASSWORD!, user.POWERUNIT!);

            if (success <= 0)
            {
                ArgumentException exception = new ArgumentException($"User update procedure failed, confirm provided credentials are valid.");
                _logger.LogError(exception, "Error updating user {Username}", user.USERNAME);
                return StatusCode(500, "Internal error generating updating new user credentials.");
            }

            return Ok(new { success });
        }

        [HttpPost]
        [Route("{username}/{company}")]
        public async Task<IActionResult> Company(string username, string company)
        {
            string? activeCompany = await _userService.SetCompanyAsync(username, company);
            if (activeCompany == null)
            {
                return BadRequest(new { message = "Company reference missing in user records, contact administrator." });
            }

            Response.Cookies.Append("company", activeCompany, _cookieService.AccessOptions());

            if (activeCompany == company)
            {
                return Ok(new { company = activeCompany, message = "Existing company remains in active slot (ie: index 0)." });
            }
            else
            {
                return Ok(new { company = activeCompany, message = "New company was placed into active slot (ie: index 0)." });
            }
        }
    }
}
