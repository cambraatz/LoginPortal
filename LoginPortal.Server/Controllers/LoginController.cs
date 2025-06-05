/*/////////////////////////////////////////////////////////////////////////////
 
Author: Cameron Braatz
Date: 11/15/2024
Update: 4/8/2025

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
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ITokenService _tokenService;
        private readonly ILogger<LoginController> _logger;
        private readonly ICookieService _cookieService;
        private readonly string? _connString;

        public LoginController(IConfiguration configuration, 
            ITokenService tokenService, 
            ILogger<LoginController> logger, 
            ICookieService cookieService)
        {
            _configuration = configuration;
            _tokenService = tokenService;
            _connString = _configuration.GetConnectionString("TCSWEB");
            _logger = logger;
            _cookieService = cookieService;
        }

        // fetch full user credentials to auto-login when return cookie is found...

        /*/////////////////////////////////////////////////////////////////////////////
 
        PullCredentials()

        Leverages active cookies to ensure valid redirects

        *//////////////////////////////////////////////////////////////////////////////
        
        /*[HttpPost]
        [Route("PullCredentials")]
        public async Task<JsonResult> PullCredentials()
        {
            // check for result and clear if found...
            bool result;
            if (!bool.TryParse(Request.Cookies["return"], out result))
            {
                return new JsonResult(new { success = false, message = "Valid return boolean not found." });
            }
            Response.Cookies.Append("return", "", _cookieService.RemoveOptions());

            // check for username if present...
            string? username = Request.Cookies["username"];
            if (string.IsNullOrEmpty(username))
            {
                return new JsonResult(new { success = false, message = "Username was not found in cookies." });
            }

            // SQL query...
            string query = @"select USERNAME, PERMISSIONS, POWERUNIT,
                            COMPANYKEY01, COMPANYKEY02, COMPANYKEY03, COMPANYKEY04, COMPANYKEY05,
                            MODULE01, MODULE02, MODULE03, MODULE04, MODULE05, MODULE06, MODULE07, MODULE08, MODULE09, MODULE10
                            from dbo.USERS where USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @USERNAME;";

            // SQL utilities...
            DataTable table = new DataTable();
            string? sqlDatasource = _connString;
            SqlDataReader myReader;

            // SQL query...
            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                await myCon.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand(query, myCon))
                {
                    // query database...
                    myCommand.Parameters.AddWithValue("@USERNAME", username);
                    myReader = await myCommand.ExecuteReaderAsync();

                    if (await myReader.ReadAsync())
                    {
                        // initialize user values...
                        User user = new User
                        {
                            Username = myReader["USERNAME"] != DBNull.Value ? myReader["USERNAME"].ToString() : null,
                            Permissions = myReader["PERMISSIONS"] != DBNull.Value ? myReader["PERMISSIONS"].ToString() : null,
                            Powerunit = myReader["POWERUNIT"] != DBNull.Value ? myReader["POWERUNIT"].ToString() : null,
                            ActiveCompany = myReader["COMPANYKEY01"] != DBNull.Value ? myReader["COMPANYKEY01"].ToString() : null,
                            Companies = new List<string>(),
                            Modules = new List<string>(),
                        };

                        // initialize company and modules arrays...
                        string companyKey;
                        string moduleKey;
                        for (int i = 1; i <= 10; i++)
                        {
                            if (i <= 5)
                            {
                                companyKey = "COMPANYKEY" + i.ToString("D2");
                                if (myReader[companyKey] != DBNull.Value || myReader[companyKey] != null)
                                {
                                    user.Companies.Add(myReader[companyKey].ToString());
                                }
                            }

                            moduleKey = "MODULE" + i.ToString("D2");
                            if (myReader[moduleKey] != DBNull.Value)
                            {
                                user.Modules.Add(myReader[moduleKey].ToString());
                            }

                        };

                        // generate token...
                        //var tokenService = new TokenService(_configuration);
                        (string accessToken, string refreshToken) = _tokenService.GenerateToken(username);

                        // cache app data in cookies...
                        Response.Cookies.Append("access_token", accessToken, _cookieService.AccessOptions());
                        Response.Cookies.Append("refresh_token", refreshToken, _cookieService.RefreshOptions());
                        Response.Cookies.Append("username", user.Username, _cookieService.AccessOptions());
                        Response.Cookies.Append("company", user.ActiveCompany, _cookieService.AccessOptions());

                        return new JsonResult(new
                        {
                            success = true,
                            user = user
                        });
                    }
                    else
                    {
                        return new JsonResult(new { success = false, message = "Invalid Credentials" });
                    }
                }
            }
        }*/

        /*/////////////////////////////////////////////////////////////////////////////
 
        Logout()

        Iteratively 'expire' all active cookies, leaving a clean slate for new users.
        Successful logout return Ok status to frontent.

        *//////////////////////////////////////////////////////////////////////////////
        
        /*[HttpPost]
        [Route("Logout")]
        public IActionResult Logout()
        {
            foreach (var cookie in Request.Cookies)
            {
                Response.Cookies.Append(cookie.Key, "", _cookieService.RemoveOptions());
            }
            return Ok(new { message = "Logged out successfully" });
        }*/

        /*/////////////////////////////////////////////////////////////////////////////
 
        FetchMappings()

        Utilized helper function to query/dump contents of dbo.COMPANY and dbo.MODULES,
        to be used in mapping short-hand keys (passed throughout sesssions) and their 
        verbose equivalents for rendering.

        *//////////////////////////////////////////////////////////////////////////////
        /*private async Task<string> _fetchMappings(string dbTable)
        {
            // initialize dictionary...
            var dict = new Dictionary<string, string>();
            string sqlDatasource = _connString;
            
            // establish SQL connection...
            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    // open SQL connection and dump table contents (company or module)...
                    await myCon.OpenAsync();
                    using (SqlCommand myCommand = new SqlCommand($"select * from dbo.{dbTable}", myCon))
                    {
                        using (SqlDataReader myReader = myCommand.ExecuteReader())
                        {
                            // access results in reader table...
                            DataTable table = new DataTable();
                            table.Load(myReader);

                            // define key and value columns associated with each DB...
                            string keyColumn = (dbTable == "COMPANY") ? "COMPANYKEY" : "MODULEURL";
                            string valColumn = (dbTable == "COMPANY") ? "COMPANYNAME" : "MODULENAME";

                            // iterate the results of the DB query...
                            foreach (DataRow row in table.Rows)
                            {
                                // ensure both are non-null before adding to dict...
                                string? key = row[keyColumn].ToString();
                                string? value = row[valColumn].ToString();
                                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                                {
                                    dict[key] = value;
                                }
                            }
                        }
                    }
                    // close connection and return dictionary...
                    await myCon.CloseAsync();
                    return JsonSerializer.Serialize(dict);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error occurred while fetching {dbTable} mappings; Exception: {ex.Message}");
                    return $"An error occurred while fetching {dbTable} mappings; Exception: {ex.Message}";
                }
            }
        }

        // fetch company and module mappings for in-app translation...
        [HttpPost]
        [Route("FetchMappings")]
        public async Task<JsonResult> FetchMappings()
        {
            try
            {
                // retrieve companies/modules JSON dict from DB...
                string companies = await _fetchMappings("COMPANY");
                string modules = await _fetchMappings("MODULE");

                // handle errors in helper utility...
                if (companies.Contains("error") || modules.Contains("error"))
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Error: _fetchMappings() internal helper returned invalid results."
                    });
                }

                // cache validated results in cookies and return results for client storage...
                else
                {
                    Response.Cookies.Append("company_mapping", companies, _cookieService.AccessOptions());
                    Response.Cookies.Append("module_mapping", modules, _cookieService.AccessOptions());

                    return new JsonResult(new
                    {
                        success = true,
                        companies = companies,
                        modules = modules,
                        message = "Company and Module mappings have been stored in cookies."
                    });
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"Company and Module mappings failed; Exception: {ex.Message}"
                });
            }
        }*/

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

        /*[HttpPost]
        [Route("Login")]
        public async Task<JsonResult> Login([FromBody] loginCredentials credentials)
        {
            string query = @" select USERNAME, PERMISSIONS, POWERUNIT,
                            COMPANYKEY01, COMPANYKEY02, COMPANYKEY03, COMPANYKEY04, COMPANYKEY05,
                            MODULE01, MODULE02, MODULE03, MODULE04, MODULE05, MODULE06, MODULE07, MODULE08, MODULE09, MODULE10
                            from dbo.USERS where USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @USERNAME
                            and PASSWORD COLLATE SQL_Latin1_General_CP1_CS_AS = @PASSWORD";

            DataTable table = new DataTable();
            string sqlDatasource = _connString;
            SqlDataReader myReader;

            try
            {
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
                var row = table.Rows[0];

                // initialize user values...
                User user = new User
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

                // validate the user has access to at least one company & module...
                if (user.Companies.Count > 0 && user.Modules.Count > 0)
                {
                    // generate tokens...
                    //var tokenService = new TokenService(_configuration);
                    (string accessToken, string refreshToken) = _tokenService.GenerateToken(credentials.USERNAME);

                    // catch tokens and user data 
                    Response.Cookies.Append("access_token", accessToken, _cookieService.AccessOptions());
                    Response.Cookies.Append("refresh_token", refreshToken, _cookieService.RefreshOptions());
                    Response.Cookies.Append("username", user.Username, _cookieService.AccessOptions());
                    Response.Cookies.Append("company", user.ActiveCompany, _cookieService.AccessOptions());

                    // return user object on success...
                    return new JsonResult(new
                    {
                        success = true,
                        user = user
                    });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "No results found for company and/or module permissions for the current user." });
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Cookie not found:" + ex });
            }
        }*/

        public class SetCompanyRequest
        {
            public string Username { get; set; }
            public string Company { get; set; }
        }

        /*[HttpPost]
        [Route("SetCompany")]
        public async Task<JsonResult> SetCompany([FromBody] SetCompanyRequest request)
        {
            try
            {
                Response.Cookies.Append("company", request.Company, _cookieService.AccessOptions());

                string query = @"select COMPANYKEY01, COMPANYKEY02, COMPANYKEY03, COMPANYKEY04, COMPANYKEY05
                            from dbo.USERS where USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @USERNAME";

                DataTable table = new DataTable();
                string sqlDatasource = _configuration.GetConnectionString("DriverChecklistTestCon");
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

                            return new JsonResult(new { success = true, company = request.Company, message = "New company was placed into active slot (ie: index 0)." });
                        }
                        else
                        {
                            return new JsonResult(new { success = true, company = request.Company, message = "Existing company remains in active slot (ie: index 0)." });
                        }
                    }
                    else
                    {
                        return new JsonResult(new { success = false, message = "No companies were found associated to the current user." });
                    }
                }                
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Cookie not found:" + ex });
            }
        }*/

        /*[HttpPost]
        [Route("PullDriver")]
        public async Task<JsonResult> PullDriver([FromBody] driverRequest request)
        {
            string query = "SELECT USERNAME, PASSWORD, POWERUNIT FROM dbo.USERS WHERE USERNAME = @USERNAME";

            DataTable table = new DataTable();
            string sqlDatasource = _connString;
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

                    // validate password...
                    bool valid = driver.PASSWORD == null || driver.PASSWORD == "" ? false : true;
                    return new JsonResult(new
                    {
                        success = true,
                        username = driver.USERNAME,
                        password = valid,
                        powerunit = driver.POWERUNIT
                    });
                }
                catch (Exception ex)
                {
                    return new JsonResult(new { success = false, error = ex.Message });
                }
            }
        }*/

        /*
        [HttpPut]
        [Route("InitializeDriver")]
        public async Task<JsonResult> InitializeDriver([FromBody] driverCredentials user)
        {
            //var tokenService = new TokenService(_configuration);
            (bool success, string message) tokenAuth = _tokenService.AuthorizeRequest(HttpContext);
            if (!tokenAuth.success)
            {
                UnauthorizedAccessException exception = new UnauthorizedAccessException($"Token authorization failed, {tokenAuth.message}");
                _logger.LogError(exception, exception.Message);
                return new JsonResult(new { success = false, message = exception.Message }) { StatusCode = StatusCodes.Status401Unauthorized };
            }

            var username = Request.Cookies["username"];
            if (username == null)
            {
                ArgumentNullException exception = new ArgumentNullException($"Failed to find 'username' from cookies");
                _logger.LogError(exception, exception.Message);
                return new JsonResult(new { success = false, message = exception.Message }) { StatusCode = StatusCodes.Status401Unauthorized };
            }

            string updateQuery = "UPDATE dbo.USERS SET PASSWORD=@PASSWORD, POWERUNIT=@POWERUNIT WHERE USERNAME=@USERNAME";

            DataTable table = new DataTable();
            string sqlDatasource = _connString;

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
        }*/
    }
}
