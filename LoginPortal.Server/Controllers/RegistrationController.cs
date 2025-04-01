/*/////////////////////////////////////////////////////////////////////////////
 
Author: Cameron Braatz
Date: 11/15/2024
Update: 1/9/2025

*//////////////////////////////////////////////////////////////////////////////

using DeliveryManager.Server.Models;
using LoginPortal.Server.Models;
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

// add utility to generate JWTs...
public class TokenService
{
    private readonly IConfiguration _configuration;
    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public (string AccessToken, string RefreshToken) GenerateToken(string username, string company=null)
    {
        var accessClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // add company if present...
        if (!string.IsNullOrEmpty(company))
        {
            accessClaims.Add(new Claim("Company", company));
        }

        var refreshClaims = new List<Claim>
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
}

public class RefreshRequest
{
    public string Username { get; set; }
    public string RefreshToken { get; set; }
}

public class CompanyRequest
{
    public string Company { get; set; }
    public string AccessToken { get; set; }
}

/*/////////////////////////////////////////////////////////////////////////////
 
Registration Controller API Functions

API endpoint functions that handle standard user credential verification, 

API Endpoints (...api/Registration/*):
    Login: check database for matching username/password combo, divert admins
    

*//////////////////////////////////////////////////////////////////////////////

namespace LoginPortal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegistrationController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        //private readonly TokenService _tokenService;
        private readonly ILogger<RegistrationController> _logger;
        private readonly string connString;

        public RegistrationController(IConfiguration configuration, TokenService tokenService, ILogger<RegistrationController> logger)
        {
            _configuration = configuration;
            //_tokenService = tokenService;
            //connString = _configuration.GetConnectionString("DriverChecklistTestCon");
            connString = _configuration.GetConnectionString("DriverChecklistDBCon");
            _logger = logger;
            //connString = _configuration.GetConnectionString("TCSWEB");
        }

        [HttpPost]
        [Route("RefreshToken")]
        public IActionResult RefreshToken([FromBody] RefreshRequest request)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(request.RefreshToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = false
            }, out SecurityToken validatedToken);

            if (validatedToken is JwtSecurityToken jwtToken &&
                jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                var tokenService = new TokenService(_configuration);
                var tokens = tokenService.GenerateToken(request.Username);
                return Ok(new { AccessToken = tokens.AccessToken, RefreshToken = tokens.RefreshToken });
            }
            return Unauthorized("Invalid Token.");
        }

        [HttpPost]
        [Route("SelectCompany")]
        [Authorize]
        public IActionResult SelectCompany([FromBody] CompanyRequest request)
        {
            // extract username from access token...
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(request.AccessToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = false
            }, out SecurityToken validatedToken);

            if (validatedToken is JwtSecurityToken jwtToken &&
                jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                var tokenService = new TokenService(_configuration);
                var tokens = tokenService.GenerateToken(principal.Identity.Name, request.Company);
                return Ok(new { AccessToken = tokens.AccessToken, RefreshToken = tokens.RefreshToken });
            }
            return Unauthorized("Invalid Token.");
        }

        [HttpPost]
        [Route("GetCookie")]
        public IActionResult GetCookie([FromBody] string key)
        {
            var value = Request.Cookies[key];
            if (string.IsNullOrEmpty(value))
            {
                return new JsonResult(new { success = false, value = $"{key} cookie was not found." });
            }

            return new JsonResult(new { success = true, value = value });
        }

        [HttpPost]
        [Route("GetUser")]
        public IActionResult GetUser()
        {
            bool result;
            if (bool.TryParse(Request.Cookies["return"], out result))
            {
                return new JsonResult(new { success = false, message = "Invalid return boolean cookie." });
            }
            string username = Request.Cookies["username"];
            if (string.IsNullOrEmpty(username))
            {
                return new JsonResult(new { success = false, message = "Invalid username cookie." });
            }
            return new JsonResult(new { success = true, user = username, message = "Valid return boolean cookie found." });
        }

        // INACTIVE?????????
        [HttpPost]
        [Route("PullCredentials")]
        public async Task<JsonResult> PullCredentials()
        {
            bool result;
            if (!bool.TryParse(Request.Cookies["return"], out result))
            {
                return new JsonResult(new { success = false, message = "Valid return boolean not found." });
            }

            Response.Cookies.Append("return", "", new CookieOptions
            {
                HttpOnly = true, // Makes it inaccessible to JavaScript
                Secure = true, // Ensures the cookie is only sent over HTTPS
                SameSite = SameSiteMode.None, // Allows sharing across subdomains
                Domain = ".tcsservices.com", // Cookie available for all subdomains of domain.com
                Expires = DateTimeOffset.UtcNow.AddDays(-1), // Set expiry for access token (e.g., 15 minutes)
                Path = "/"
            });

            string username = Request.Cookies["username"];
            if (string.IsNullOrEmpty(username))
            {
                return new JsonResult(new { success = false, message = "Username was not found in cookies." });
            }

            string query = @"select USERNAME, PERMISSIONS, POWERUNIT,
                            COMPANYKEY01, COMPANYKEY02, COMPANYKEY03, COMPANYKEY04, COMPANYKEY05,
                            MODULE01, MODULE02, MODULE03, MODULE04, MODULE05, MODULE06, MODULE07, MODULE08, MODULE09, MODULE10
                            from dbo.USERS where USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @USERNAME;";

            DataTable table = new DataTable();
            string sqlDatasource = connString;
            SqlDataReader myReader;

            User user = null;

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                myCon.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    myCommand.Parameters.AddWithValue("@USERNAME", username);

                    myReader = myCommand.ExecuteReader();
                    table.Load(myReader);
                    myReader.Close();
                    myCon.Close();
                }
            }
            if (table.Rows.Count > 0)
            {
                var row = table.Rows[0];

                // initialize user values...
                user = new User
                {
                    Username = row["USERNAME"] != DBNull.Value ? row["USERNAME"].ToString() : null,
                    Permissions = row["PERMISSIONS"] != DBNull.Value ? row["PERMISSIONS"].ToString() : null,
                    Powerunit = row["POWERUNIT"] != DBNull.Value ? row["POWERUNIT"].ToString() : null,
                    ActiveCompany = row["COMPANYKEY01"] != DBNull.Value ? row["COMPANYKEY01"].ToString() : null,
                    Companies = new List<string>(),
                    Modules = new List<string>(),
                };

                // populate COMPANY + MODULE arrays...
                string companyKey;
                string moduleKey;
                for (int i = 1; i <= 10; i++)
                {
                    if (i <= 5)
                    {
                        companyKey = "COMPANYKEY" + i.ToString("D2");
                        if (row[companyKey] != DBNull.Value)
                        {
                            user.Companies.Add(row[companyKey].ToString());
                        }
                    }

                    moduleKey = "MODULE" + i.ToString("D2");
                    if (row[moduleKey] != DBNull.Value)
                    {
                        user.Modules.Add(row[moduleKey].ToString());
                    }

                };

                // generate token...
                var tokenService = new TokenService(_configuration);
                (string accessToken, string refreshToken) = tokenService.GenerateToken(username);

                //var isHttps = Request.IsHttps;

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true, // Makes it inaccessible to JavaScript
                    Secure = true, // Ensures the cookie is only sent over HTTPS
                    SameSite = SameSiteMode.None, // Allows sharing across subdomains
                    Domain = ".tcsservices.com", // Cookie available for all subdomains of domain.com
                    Expires = DateTimeOffset.UtcNow.AddMinutes(15), // Set expiry for access token (e.g., 15 minutes)
                    Path = "/"
                };

                Response.Cookies.Append("access_token", accessToken, cookieOptions);
                Response.Cookies.Append("username", user.Username, cookieOptions);
                Response.Cookies.Append("company", user.ActiveCompany, cookieOptions);

                cookieOptions.Expires = DateTimeOffset.UtcNow.AddDays(1);
                Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);

                /*
                Response.Cookies.Append("user", user.Username, cookieOptions);
                Response.Cookies.Append("powerunit", user.Powerunit, cookieOptions);
                Response.Cookies.Append("company", user.ActiveCompany, cookieOptions);
                Response.Cookies.Append("accessToken", accessToken, cookieOptions);
                Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
                */

                return new JsonResult(new
                {
                    success = true,
                    user = user,
                    accessToken = accessToken,
                    refreshToken = refreshToken
                });
            }
            else
            {
                return new JsonResult(new { success = false, message = "Invalid Credentials" });
            }
        }

        /*/////////////////////////////////////////////////////////////////////////////
 
        Login(username, password)

        Handles both driver and administrator login credentials, sending the former to
        the standard application flow and the latter to the admin portal.

        Queries the USERS database table for any users matching the provided username 
        and password combination provided by the user. On successful query, declare the
        task (ie: admin/driver) and return the powerunit on record when logging into a
        valid driver account.

        *//////////////////////////////////////////////////////////////////////////////

        [HttpPost]
        [Route("Login")]
        public async Task<JsonResult> Login([FromBody] loginCredentials credentials)
        {
            //string query = "select * from dbo.USERS where USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @USERNAME and PASSWORD COLLATE SQL_Latin1_General_CP1_CS_AS = @PASSWORD";
            string query = @" select USERNAME, PERMISSIONS, POWERUNIT,
                            COMPANYKEY01, COMPANYKEY02, COMPANYKEY03, COMPANYKEY04, COMPANYKEY05,
                            MODULE01, MODULE02, MODULE03, MODULE04, MODULE05, MODULE06, MODULE07, MODULE08, MODULE09, MODULE10
                            from dbo.USERS where USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @USERNAME
                            and PASSWORD COLLATE SQL_Latin1_General_CP1_CS_AS = @PASSWORD";

            DataTable table = new DataTable();
            string sqlDatasource = connString;
            SqlDataReader myReader;

            User user = null;

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    myCon.Open();
                    myCommand.Parameters.AddWithValue("@USERNAME", credentials.USERNAME);
                    myCommand.Parameters.AddWithValue("@PASSWORD", credentials.PASSWORD);

                    myReader = myCommand.ExecuteReader();
                    table.Load(myReader);
                    myReader.Close();
                    myCon.Close();
                }
            }
            if (table.Rows.Count > 0)
            {
                var row = table.Rows[0];

                // initialize user values...
                user = new User
                {
                    Username = row["USERNAME"] != DBNull.Value ? row["USERNAME"].ToString() : null,
                    Permissions = row["PERMISSIONS"] != DBNull.Value ? row["PERMISSIONS"].ToString() : null,
                    Powerunit = row["POWERUNIT"] != DBNull.Value ? row["POWERUNIT"].ToString() : null,
                    ActiveCompany = row["COMPANYKEY01"] != DBNull.Value ? row["COMPANYKEY01"].ToString() : null,
                    Companies = new List<string>(),
                    Modules = new List<string>(),
                };

                // populate COMPANY + MODULE arrays...
                string companyKey;
                string moduleKey;
                for (int i = 1; i <= 10; i++)
                {
                    if (i <= 5)
                    {
                        companyKey = "COMPANYKEY" + i.ToString("D2");
                        if (row[companyKey] != DBNull.Value)
                        {
                            user.Companies.Add(row[companyKey].ToString());
                        }
                    }

                    moduleKey = "MODULE" + i.ToString("D2");
                    if (row[moduleKey] != DBNull.Value)
                    {
                        user.Modules.Add(row[moduleKey].ToString());
                    }
                        
                };

                // generate token...
                var tokenService = new TokenService(_configuration);
                (string accessToken, string refreshToken) = tokenService.GenerateToken(credentials.USERNAME);

                //var isHttps = Request.IsHttps;

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true, // Makes it inaccessible to JavaScript
                    Secure = true, // Ensures the cookie is only sent over HTTPS
                    SameSite = SameSiteMode.None, // Allows sharing across subdomains
                    Domain = ".tcsservices.com", // Cookie available for all subdomains of domain.com
                    Expires = DateTimeOffset.UtcNow.AddMinutes(15), // Set expiry for access token (e.g., 15 minutes)
                    Path = "/"
                };

                Response.Cookies.Append("access_token", accessToken, cookieOptions);
                Response.Cookies.Append("username", user.Username, cookieOptions);
                Response.Cookies.Append("company", user.ActiveCompany, cookieOptions);

                cookieOptions.Expires = DateTimeOffset.UtcNow.AddDays(1);
                Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);

                //var companies = JsonSerializer.Serialize(FetchCompanies());
                //Response.Cookies.Append("company_mapping", companies, cookieOptions);

                //var modules = JsonSerializer.Serialize(FetchModules());
                //Response.Cookies.Append("module_mapping", companies, cookieOptions);

                /*
                Response.Cookies.Append("user", user.Username, cookieOptions);
                Response.Cookies.Append("powerunit", user.Powerunit, cookieOptions);
                Response.Cookies.Append("company", user.ActiveCompany, cookieOptions);
                Response.Cookies.Append("accessToken", accessToken, cookieOptions);
                Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
                */

                return new JsonResult(new 
                { 
                    success = true,
                    user = user,
                    accessToken = accessToken,
                    refreshToken = refreshToken 
                });
            }
            else
            {
                return new JsonResult(new { success = false, message = "Invalid Credentials" });
            }
        }

        [HttpPost]
        [Route("FetchMappings")]
        public async Task<JsonResult> FetchMappings()
        {
            try
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true, // Makes it inaccessible to JavaScript
                    Secure = true, // Ensures the cookie is only sent over HTTPS
                    SameSite = SameSiteMode.None, // Allows sharing across subdomains
                    Domain = ".tcsservices.com", // Cookie available for all subdomains of domain.com
                    Expires = DateTimeOffset.UtcNow.AddMinutes(15), // Set expiry for access token (e.g., 15 minutes)
                    Path = "/"
                };
                var rawCompanies = await FetchCompanies();
                var companies = JsonSerializer.Serialize(rawCompanies);//await FetchCompanies());
                Response.Cookies.Append("company_mapping", companies, cookieOptions);

                var rawModules = await FetchModules();
                var modules = JsonSerializer.Serialize(rawModules);//await FetchModules());
                Response.Cookies.Append("module_mapping", modules, cookieOptions);

                return new JsonResult(new
                {
                    success = true,
                    companies = companies,
                    modules = modules,
                    message = "Company and Module mappings have been stored in cookies."
                });
            } catch (Exception ex) {
                return new JsonResult(new
                {
                    success = false,
                    message = $"Company and Module mappings failed; Exception: {ex.Message}"
                });
            }
        }

        private async Task<Dictionary<string, string>?> FetchCompanies()
        {
            var companies = new Dictionary<string,string>();
            string sqlDatasource = connString;

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    await myCon.OpenAsync();
                    using (SqlCommand myCommand = new SqlCommand("select * from dbo.COMPANY", myCon))
                    {
                        using (SqlDataReader myReader = myCommand.ExecuteReader())
                        {
                            DataTable table = new DataTable();
                            table.Load(myReader);

                            foreach (DataRow row in table.Rows)
                            {
                                var key = row["COMPANYKEY"].ToString();
                                var name = row["COMPANYNAME"].ToString();

                                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(name))
                                {
                                    companies[key] = name;
                                }
                            }
                        }
                    }

                    await myCon.CloseAsync();
                    return companies;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error occurred while fetching companies; Exception: {ex.Message}");
                    return null;
                }
            }
        }
        private async Task<Dictionary<string, string>?> FetchModules()
        {
            var modules = new Dictionary<string, string>();
            string sqlDatasource = connString;

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    await myCon.OpenAsync();
                    using (SqlCommand myCommand = new SqlCommand("select * from dbo.MODULE", myCon))
                    {
                        using (SqlDataReader myReader = myCommand.ExecuteReader())
                        {
                            DataTable table = new DataTable();
                            table.Load(myReader);

                            foreach (DataRow row in table.Rows)
                            {
                                var key = row["MODULEURL"].ToString();
                                var name = row["MODULENAME"].ToString();

                                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(name))
                                {
                                    modules[key] = name;
                                }
                            }
                        }
                    }
                    await myCon.CloseAsync();
                    return modules;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error occurred while fetching modules; Exception: {ex.Message}");
                    return null;
                }
            }
        }

        [HttpPost]
        [Route("Logout")]

        public IActionResult Logout()
        {
            CookieOptions options = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(-1),
                HttpOnly = true,
                Secure = true,
                Domain = ".tcsservices.com",
                SameSite = SameSiteMode.None,
                Path = "/"
            };

            foreach (var cookie in Request.Cookies)
            {
                Response.Cookies.Append(cookie.Key, "", options);
            }

            /*Response.Cookies.Append("access_token", "", options);
            Response.Cookies.Append("refresh_token", "", options);
            Response.Cookies.Append("username", "", options);
            Response.Cookies.Append("company", "", options);
            Response.Cookies.Append("return", "", options);*/

            /*foreach (var cookie in Request.Cookies) 
            {
                Response.Cookies.Append(cookie.Key,cookie.Value,options);
            }*/

            return Ok(new { message = "Logged out successfully" });
        }
        public class SetCompanyRequest
        {
            public string Username { get; set; }
            public string Company { get; set; }
        }

        [HttpPost]
        [Route("SetCompany")]
        public async Task<JsonResult> SetCompany([FromBody] SetCompanyRequest request)
        {
            try
            {
                Response.Cookies.Append("company", request.Company, new CookieOptions
                {
                    HttpOnly = true, // Makes it inaccessible to JavaScript
                    Secure = true, // Ensures the cookie is only sent over HTTPS
                    SameSite = SameSiteMode.None, // Allows sharing across subdomains
                    Domain = ".tcsservices.com", // Cookie available for all subdomains of domain.com
                    Expires = DateTimeOffset.UtcNow.AddMinutes(15), // Set expiry for access token (e.g., 15 minutes)
                    Path = "/"
                });

                string query = @" select COMPANYKEY01, COMPANYKEY02, COMPANYKEY03, COMPANYKEY04, COMPANYKEY05
                            from dbo.USERS where USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @USERNAME";

                DataTable table = new DataTable();
                string sqlDatasource = connString;
                SqlDataReader myReader;

                await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
                {
                    myCon.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@USERNAME", request.Username);

                        myReader = myCommand.ExecuteReader();
                        table.Load(myReader);
                        myReader.Close();
                    }

                    if (table.Rows.Count > 0)
                    {
                        DataRow row = table.Rows[0];
                        List<string> companies = new List<string>
                        {
                            row["COMPANYKEY01"]?.ToString(),
                            row["COMPANYKEY02"]?.ToString(),
                            row["COMPANYKEY03"]?.ToString(),
                            row["COMPANYKEY04"]?.ToString(),
                            row["COMPANYKEY05"]?.ToString()
                        };

                        int index = companies.IndexOf(request.Company);

                        if (index > 0)
                        {
                            string swapColumn = $"COMPANYKEY0{index + 1}";
                            string prevCompany = companies[0];

                            (companies[0], companies[index]) = (companies[index], companies[0]);

                            string updateQuery = $@" update dbo.USERS set COMPANYKEY01 = @NewCompany01, {swapColumn} = @NewCompanyOther
                                                where USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @USERNAME";

                            using (SqlCommand myCommand = new SqlCommand(updateQuery, myCon))
                            {
                                myCommand.Parameters.AddWithValue("NewCompany01", companies[0]);
                                myCommand.Parameters.AddWithValue("NewCompanyOther", companies[index]);
                                myCommand.Parameters.AddWithValue("@USERNAME", request.Username);

                                myCommand.ExecuteNonQuery();
                            }
                        }
                    }
                }
                var contents = Request.Cookies["company"];
                if (contents != null && contents == request.Company)
                {
                    return new JsonResult(new { success = true, company = contents });
                }
                return new JsonResult(new { success = false, message = "Cookie did not update" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Cookie not found:" + ex });
            }
        }

        [HttpGet]
        [Route("GetCompanies")]
        [Authorize]
        public async Task<JsonResult> GetCompanies()
        {
            //string query = "SELECT COMPANYKEY01, COMPANYKEY02, COMPANYKEY03, COMPANYKEY04, COMPANYKEY05 FROM dbo.USERS WHERE USERNAME = @USERNAME";
            string query = "SELECT * FROM dbo.COMPANY";

            string sqlDatasource = connString;
            List<Company> companies = new List<Company>();

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    await myCon.OpenAsync();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        using (SqlDataReader myReader = await myCommand.ExecuteReaderAsync())
                        {
                            while (await myReader.ReadAsync())
                            {
                                Company company = new Company
                                {
                                    COMPANYKEY = myReader["COMPANYKEY"].ToString(),
                                    COMPANYNAME = myReader["COMPANYNAME"].ToString(),
                                };
                                companies.Add(company);
                            }
                            if (companies.Any())
                            {

                                return new JsonResult(new { success = true, companies });
                            }
                            else
                            {
                                return new JsonResult(new { success = false, message = "No companies found..." });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return new JsonResult(new { success = false, error = ex.Message });
                }
            }
        }

        /*/////////////////////////////////////////////////////////////////////////////

        VerifyPowerunit(driverVerification driver)

        Token-protected verification of powerunit/delivery date combination existence
        in database. Handling of success/fail is handled with frontend logic.

        *//////////////////////////////////////////////////////////////////////////////

        // LOGIN FUNCTION...
        [HttpPost]
        [Route("VerifyPowerunit")]
        [Authorize]
        public async Task<JsonResult> VerifyPowerunit([FromBody] driverVerification driver)
        {
            string updatequery = "update dbo.USERS set POWERUNIT=@POWERUNIT where USERNAME=@USERNAME";
            string selectquery = "select * from dbo.DMFSTDAT where MFSTDATE=@MFSTDATE and POWERUNIT=@POWERUNIT";

            DataTable table = new DataTable();
            string sqlDatasource = connString;
            SqlDataReader myReader;

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    myCon.Open();

                    using (SqlCommand myCommand = new SqlCommand(updatequery, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@USERNAME", driver.USERNAME);
                        myCommand.Parameters.AddWithValue("@PASSWORD", driver.PASSWORD);
                        myCommand.Parameters.AddWithValue("@POWERUNIT", driver.POWERUNIT);
                        // need to include previous COMPANYKEY and MODULE values here...

                        myReader = myCommand.ExecuteReader();
                        table.Load(myReader);
                        myReader.Close();
                    }

                    using (SqlCommand myCommand = new SqlCommand(selectquery, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@MFSTDATE", driver.MFSTDATE);
                        myCommand.Parameters.AddWithValue("@POWERUNIT", driver.POWERUNIT);
                        //myCommand.ExecuteNonQuery();

                        myReader = myCommand.ExecuteReader();
                        table.Load(myReader);
                        myReader.Close();
                    }
                    myCon.Close();

                    if (table.Rows.Count > 0)
                    {
                        return new JsonResult(new { success = true });
                    }
                    else
                    {
                        return new JsonResult(new { success = false });
                    }
                }
                catch (Exception ex)
                {
                    return new JsonResult("Error: " + ex.Message);
                }
            }
        }

        /*/////////////////////////////////////////////////////////////////////////////
 
        GetAllDrivers() - INACTIVE

        Helper function used in debugging, originally used in the context of a 'dump 
        users' button to dump all current users to console for review.

        *//////////////////////////////////////////////////////////////////////////////

        [HttpGet]
        [Route("GetAllDrivers")]
        [Authorize]
        public JsonResult GetAllDrivers()
        {
            string query = "select * from dbo.USERS";

            DataTable table = new DataTable();
            string sqlDatasource = connString;
            SqlDataReader myReader;

            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                myCon.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    myReader = myCommand.ExecuteReader();
                    table.Load(myReader);
                    myReader.Close();
                    myCon.Close();
                }
            }
            return new JsonResult(new { success = true, table = table });
        }

        // is this still active???
        // ADMIN FUNCTION...
        [HttpPut]
        [Route("UpdateDriver")]
        [Authorize]
        public async Task<JsonResult> UpdateDriver([FromForm] driverCredentials user)
        {
            string query = "update dbo.USERS set USERNAME=@USERNAME, PASSWORD=@PASSWORD, POWERUNIT=@POWERUNIT where USERNAME=@USERNAME";

            DataTable table = new DataTable();
            string sqlDatasource = connString;
            SqlDataReader myReader;

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                myCon.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    myCommand.Parameters.AddWithValue("@USERNAME", user.USERNAME);
                    myCommand.Parameters.AddWithValue("@PASSWORD", user.PASSWORD);
                    myCommand.Parameters.AddWithValue("@POWERUNIT", user.POWERUNIT);

                    myReader = myCommand.ExecuteReader();
                    table.Load(myReader);
                    myReader.Close();
                    myCon.Close();
                }
            }
            return new JsonResult("Updated Successfully");
        }

        // ADMIN FUNCTION...
        [HttpPut]
        [Route("AddDriver")]
        public async Task<JsonResult> AddDriver([FromForm] string USERNAME, [FromForm] string PASSWORD, [FromForm] string POWERUNIT)
        {
            string insertQuery = "INSERT INTO dbo.USERS(USERNAME, PASSWORD, POWERUNIT) VALUES (@USERNAME, @PASSWORD, @POWERUNIT)";
            string selectQuery = "SELECT * FROM dbo.USERS";

            DataTable table = new DataTable();
            string sqlDatasource = connString;

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    // open the db connection...
                    myCon.Open();

                    // insert new user to dbo.USERS...
                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, myCon))
                    {
                        insertCommand.Parameters.AddWithValue("@USERNAME", USERNAME);
                        insertCommand.Parameters.AddWithValue("@PASSWORD", DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@POWERUNIT", POWERUNIT);

                        int insertResponse = await insertCommand.ExecuteNonQueryAsync();

                        // check if new user was inserted successfully...
                        if (insertResponse <= 0)
                        {
                            return new JsonResult("Error creating new user.");
                        }
                    }

                    // gather the new table contents for dbo.USERS...
                    using (SqlCommand selectCommand = new SqlCommand(selectQuery, myCon))
                    {
                        SqlDataAdapter adapter = new SqlDataAdapter(selectCommand);
                        adapter.Fill(table);
                    }

                    // close db connection...
                    myCon.Close();

                    // return success message...
                    return new JsonResult(new { success = true, table = table });
                }
                catch (Exception ex)
                {
                    //return new JsonResult("Error: " + ex.Message);
                    return new JsonResult(new { success = false, error = ex.Message });
                }
            }
        }

        // ADMIN + LOGIN FUNCTION...
        [HttpPut]
        [Route("ReplaceDriver")]
        [Authorize]
        public async Task<JsonResult> ReplaceDriver([FromBody] driverReplacement driver)
        {
            string deleteQuery = "DELETE FROM dbo.USERS WHERE USERNAME = @PREVUSER";
            string insertQuery = "INSERT INTO dbo.USERS(USERNAME, PASSWORD, POWERUNIT) VALUES (@USERNAME, @PASSWORD, @POWERUNIT)";
            string selectQuery = "SELECT * FROM dbo.USERS";

            DataTable table = new DataTable();
            string sqlDatasource = connString;

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    // open the db connection...
                    myCon.Open();

                    // delete the old user from dbo.USERS...
                    using (SqlCommand deleteCommand = new SqlCommand(deleteQuery, myCon))
                    {
                        deleteCommand.Parameters.AddWithValue("@PREVUSER", driver.PREVUSER);

                        int deleteResult = await deleteCommand.ExecuteNonQueryAsync();

                        // check if old user was deleted successfully...
                        if (deleteResult <= 0)
                        {
                            return new JsonResult("Error deleting previous user, moving forward with replacing user.");
                        }
                    }

                    // insert new user to dbo.USERS...
                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, myCon))
                    {
                        insertCommand.Parameters.AddWithValue("@USERNAME", driver.USERNAME);
                        insertCommand.Parameters.AddWithValue("@PASSWORD", string.IsNullOrEmpty(driver.PASSWORD) ? DBNull.Value : driver.PASSWORD);
                        insertCommand.Parameters.AddWithValue("@POWERUNIT", driver.POWERUNIT);

                        int insertResponse = await insertCommand.ExecuteNonQueryAsync();

                        // check if new user was inserted successfully...
                        if (insertResponse <= 0)
                        {
                            return new JsonResult("Error creating new user.");
                        }
                    }

                    // gather the new table contents for dbo.USERS...
                    using (SqlCommand selectCommand = new SqlCommand(selectQuery, myCon))
                    {
                        SqlDataAdapter adapter = new SqlDataAdapter(selectCommand);
                        adapter.Fill(table);
                    }

                    // close db connection...
                    myCon.Close();

                    // return success message...
                    return new JsonResult(new { success = true, table = table });
                }
                catch (Exception ex)
                {
                    return new JsonResult("Error: " + ex.Message);
                }
            }
        }

        // ADMIN + LOGIN FUNCTION...
        [HttpPut]
        [Route("InitializeDriver")]
        public async Task<JsonResult> InitializeDriver([FromBody] driverCredentials user)
        {
            string updateQuery = "UPDATE dbo.USERS SET PASSWORD=@PASSWORD, POWERUNIT=@POWERUNIT WHERE USERNAME=@USERNAME";

            DataTable table = new DataTable();
            string sqlDatasource = connString;

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    // open the db connection...
                    myCon.Open();

                    // delete the old user from dbo.USERS...
                    using (SqlCommand updateCommand = new SqlCommand(updateQuery, myCon))
                    {
                        updateCommand.Parameters.AddWithValue("@USERNAME", user.USERNAME);
                        updateCommand.Parameters.AddWithValue("@PASSWORD", user.PASSWORD);
                        updateCommand.Parameters.AddWithValue("@POWERUNIT", user.POWERUNIT);


                        int updateResult = await updateCommand.ExecuteNonQueryAsync();

                        // check if old user was deleted successfully...
                        if (updateResult <= 0)
                        {
                            return new JsonResult(new { success = false, message = "User not found or no changes made." });
                        }
                    }

                    // close db connection...
                    myCon.Close();

                    // return success message...
                    return new JsonResult(new { success = true });
                }
                catch (Exception ex)
                {
                    return new JsonResult(new { success = false, message = "Error: " + ex.Message });
                }
            }
        }

        // ADMIN FUNCTION...
        [HttpDelete]
        [Route("DeleteDriver")]
        [Authorize]
        public JsonResult DeleteDriver(string USERNAME)
        {
            string query = "delete from dbo.USERS where USERNAME=@USERNAME";
            string selectQuery = "SELECT * FROM dbo.USERS";

            DataTable table = new DataTable();
            string sqlDatasource = connString;
            SqlDataReader myReader;

            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    myCon.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {

                        myCommand.Parameters.AddWithValue("@USERNAME", USERNAME);
                        myReader = myCommand.ExecuteReader();
                        table.Load(myReader);
                        myReader.Close();

                    }

                    // gather the new table contents for dbo.USERS...
                    using (SqlCommand selectCommand = new SqlCommand(selectQuery, myCon))
                    {
                        SqlDataAdapter adapter = new SqlDataAdapter(selectCommand);
                        adapter.Fill(table);
                    }
                    myCon.Close();

                    // return success message...
                    return new JsonResult(new { success = true });
                }
                catch (Exception ex)
                {
                    return new JsonResult(new { success = false, error = "Error: " + ex.Message });

                }
            }
        }

        // ADMIN + LOGIN FUNCTION...
        [HttpPost]
        //[HttpGet]
        [Route("PullDriver")]
        public async Task<JsonResult> PullDriver([FromBody] driverRequest request)
        {
            string query = "SELECT USERNAME, PASSWORD, POWERUNIT FROM dbo.USERS WHERE USERNAME = @USERNAME";

            DataTable table = new DataTable();
            string sqlDatasource = connString;
            driverCredentials driver = new driverCredentials();

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    myCon.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@USERNAME", request.USERNAME);
                        using (SqlDataReader myReader = await myCommand.ExecuteReaderAsync())
                        {
                            if (myReader.Read())
                            {
                                driver.USERNAME = myReader["USERNAME"].ToString();
                                driver.PASSWORD = myReader["PASSWORD"].ToString();
                                driver.POWERUNIT = myReader["POWERUNIT"].ToString();
                            }
                            myReader.Close();
                        }
                    }
                    myCon.Close();

                    // generate token...
                    var tokenService = new TokenService(_configuration);
                    (string accessToken, string refreshToken) = tokenService.GenerateToken(driver.USERNAME);

                    // validate password...
                    bool valid = driver.PASSWORD == null || driver.PASSWORD == "" ? false : true;
                    if (request.admin)
                    {
                        return new JsonResult(new {
                            success = true,
                            username = driver.USERNAME,
                            password = driver.PASSWORD,
                            powerunit = driver.POWERUNIT,
                            accessToken = accessToken,
                            refreshToken = refreshToken
                        });
                    }
                    else
                    {
                        return new JsonResult(new
                        {
                            success = true,
                            username = driver.USERNAME,
                            password = valid,
                            powerunit = driver.POWERUNIT,
                            accessToken = accessToken,
                            refreshToken = refreshToken
                        });
                    }
                }
                catch (Exception ex)
                {
                    return new JsonResult(new { success = false, error = ex.Message });
                }
            }
        }

        // ADMIN + LOGIN FUNCTION...
        [HttpGet]
        [Route("GetCompany")]
        public async Task<JsonResult> GetCompany([FromQuery] string COMPANYKEY)
        {
            string query = "SELECT * FROM dbo.COMPANY where COMPANYKEY=@COMPANYKEY";

            string sqlDatasource = connString;

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    await myCon.OpenAsync();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@COMPANYKEY", COMPANYKEY);
                        using (SqlDataReader myReader = await myCommand.ExecuteReaderAsync())
                        {
                            if (await myReader.ReadAsync())
                            {
                                Company company = new Company
                                {
                                    COMPANYKEY = myReader["COMPANYKEY"].ToString(),
                                    COMPANYNAME = myReader["COMPANYNAME"].ToString()
                                };
                                return new JsonResult(new { success = true, COMPANYKEY = company.COMPANYKEY, COMPANYNAME = company.COMPANYNAME });
                            }
                            else
                            {
                                return new JsonResult(new { success = false, message = "No company found..." });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return new JsonResult(new { success = false, error = ex.Message });
                }
            }
        }

        // ADMIN FUNCTION...
        [HttpPut]
        [Route("SetCompany")]
        [Authorize]
        public async Task<JsonResult> SetCompany([FromBody] string COMPANYNAME)
        {
            string query = "update dbo.COMPANY set COMPANYNAME=@COMPANYNAME where COMPANYKEY=@COMPANYKEY";
            string insertQuery = "insert into dbo.COMPANY (COMPANYKEY, COMPANYNAME) values (@COMPANYKEY, @COMPANYNAME)";

            DataTable table = new DataTable();
            string sqlDatasource = connString;

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    await myCon.OpenAsync();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@COMPANYNAME", COMPANYNAME);
                        myCommand.Parameters.AddWithValue("@COMPANYKEY", "c01");

                        int rowsAffected = await myCommand.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return new JsonResult(new { success = true, message = "Company Updated", COMPANYNAME });
                        }
                    }

                    using (SqlCommand myCommand = new SqlCommand(insertQuery, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@COMPANYNAME", COMPANYNAME);
                        myCommand.Parameters.AddWithValue("@COMPANYKEY", "c01");

                        int rowsInserted = await myCommand.ExecuteNonQueryAsync();

                        if (rowsInserted > 0)
                        {
                            return new JsonResult(new { success = true, message = "New Company Added", COMPANYNAME });
                        }
                        else
                        {
                            return new JsonResult(new { success = false, message = "Failed to add new company" });
                        }
                    }
                }
                catch (Exception ex)
                {
                    return new JsonResult("Error: " + ex.Message);
                }
            }
        }
    }
}
