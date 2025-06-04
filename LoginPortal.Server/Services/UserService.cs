using System.Data.SqlClient;
using System.Data;
using LoginPortal.Server.Models;
using LoginPortal.Server.Services.Interfaces;
using LoginPortal.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace LoginPortal.Server.Services
{
    public class UserService : IUserService
    {
        private readonly IConfiguration _config;
        private readonly string? _connString;
        private readonly ILogger<UserService> _logger;

        public UserService(IConfiguration config, ILogger<UserService> logger)
        {
            _config = config;
            _connString = config.GetConnectionString("TCSWEB");
            _logger = logger;
        }

        private async Task<User?> FetchUserAsync(string sqlQuery, Action<SqlParameterCollection> addParams)
        {
            await using var conn = new SqlConnection(_connString);
            await using var comm = new SqlCommand(sqlQuery, conn);
            addParams(comm.Parameters);

            await conn.OpenAsync();
            await using var reader = await comm.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (!reader.Read()) { return null; }

            var user = new User
            {
                Username = reader["USERNAME"].ToString(),
                Permissions = reader["PERMISSIONS"].ToString(),
                Powerunit = reader["POWERUNIT"].ToString(),
                ActiveCompany = reader["COMPANYKEY01"].ToString(),
                Companies = new(),
                Modules = new()
            };

            for (int i = 1; i <= 5; i++)
            {
                var key = reader[$"COMPANYKEY0{i}"]?.ToString();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    user.Companies.Add(key);
                }
            }

            for (int i = 1; i <= 10; i++)
            {
                var mod = reader[$"MODULE{i:D2}"]?.ToString();
                if (!string.IsNullOrWhiteSpace(mod))
                {
                    user.Modules.Add(mod);
                }
            }

            return user;
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            const string query = @" select USERNAME, PERMISSIONS, POWERUNIT,
                            COMPANYKEY01, COMPANYKEY02, COMPANYKEY03, COMPANYKEY04, COMPANYKEY05,
                            MODULE01, MODULE02, MODULE03, MODULE04, MODULE05, MODULE06, MODULE07, MODULE08, MODULE09, MODULE10
                            from dbo.USERS where USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @USERNAME
                            and PASSWORD COLLATE SQL_Latin1_General_CP1_CS_AS = @PASSWORD";

            User? user = await FetchUserAsync(query, p =>
            {
                p.AddWithValue("@USERNAME", username);
                p.AddWithValue("@PASSWORD", password);
            });

            return user;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            const string query = @" select USERNAME, PERMISSIONS, POWERUNIT,
                            COMPANYKEY01, COMPANYKEY02, COMPANYKEY03, COMPANYKEY04, COMPANYKEY05,
                            MODULE01, MODULE02, MODULE03, MODULE04, MODULE05, MODULE06, MODULE07, MODULE08, MODULE09, MODULE10
                            from dbo.USERS where USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @USERNAME";

            User? user = await FetchUserAsync(query, p => p.AddWithValue("@USERNAME", username));

            return user;
        }

        private async Task<int> UpdateAsync(string sqlQuery, Action<SqlParameterCollection> addParams)
        {
            await using var conn = new SqlConnection(_connString);
            await using var comm = new SqlCommand(sqlQuery, conn);
            addParams(comm.Parameters);

            await conn.OpenAsync();
            int rowsAffected = await comm.ExecuteNonQueryAsync();
            return rowsAffected;
        }

        public async Task<int> UpdateUserAsync(string username, string password, string powerunit)
        {
            string query = "UPDATE dbo.USERS SET PASSWORD=@PASSWORD, POWERUNIT=@POWERUNIT WHERE USERNAME=@USERNAME";

            int success = await UpdateAsync(query, p =>
            {
                p.AddWithValue("@USERNAME", username);
                p.AddWithValue("@PASSWORD", password);
                p.AddWithValue("@POWERUNIT", powerunit);
            });

            return success;
        }

        public async Task<string?> SetCompanyAsync(string username, string company)
        {
            string companyQuery = @"select COMPANYKEY01, COMPANYKEY02, COMPANYKEY03, COMPANYKEY04, COMPANYKEY05
                            from dbo.USERS where USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @USERNAME";

            DataTable table = new DataTable();
            SqlDataReader reader;

            await using var conn = new SqlConnection(_connString);
            await using var companyComm = new SqlCommand(companyQuery, conn);
            await conn.OpenAsync();

            companyComm.Parameters.AddWithValue("@USERNAME", username);
            reader = await companyComm.ExecuteReaderAsync();
            table.Load(reader);
            reader.Close();

            if (table.Rows.Count > 0)
            {
                DataRow row = table.Rows[0];
                List<string> companies = new List<string>
                {
                    row["COMPANYKEY01"].ToString()!,
                    row["COMPANYKEY02"]?.ToString() ?? string.Empty,
                    row["COMPANYKEY03"]?.ToString() ?? string.Empty,
                    row["COMPANYKEY04"]?.ToString() ?? string.Empty,
                    row["COMPANYKEY05"]?.ToString() ?? string.Empty
                };

                int index = companies.IndexOf(company);
                if (index > 0)
                {
                    string swap = $"COMPANYKEY0{index + 1}";
                    string prevCompany = companies[0];
                    (companies[0], companies[index]) = (companies[index], companies[0]);

                    string swapQuery = $@" update dbo.USERS set COMPANYKEY01 = @NEWCOMPANY, {swap} = @PREVCOMPANY
                                                                where USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @USERNAME";

                    await using var swapComm = new SqlCommand(swapQuery, conn);
                    swapComm.Parameters.AddWithValue("@NEWCOMPANY", companies[0]);
                    swapComm.Parameters.AddWithValue("@PREVCOMPANY", companies[index]);
                    swapComm.Parameters.AddWithValue("@USERNAME", username);

                    await swapComm.ExecuteNonQueryAsync();

                    return companies[0];
                    //return new { company = companies[0], message = "New company was placed into active slot (ie: index 0)." };
                }
                else
                {
                    return company;
                    //return new { company = company, message = "Existing company remains in active slot (ie: index 0)." };
                }
            }

            return null;
        }

    }
}
