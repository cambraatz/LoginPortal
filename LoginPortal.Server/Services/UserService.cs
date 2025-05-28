using System.Data.SqlClient;
using System.Data;
using LoginPortal.Server.Models;
using LoginPortal.Server.Services.Interfaces;
using LoginPortal.Server.Controllers;

namespace LoginPortal.Server.Services
{
    public class UserService : IUserService
    {
        private readonly string? _connString;
        private readonly ILogger<UserService> _logger;

        public UserService(IConfiguration config, ILogger<UserService> logger)
        {
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

            /*await using var conn = new SqlConnection(_connString);
            await using var comm = new SqlCommand(query, conn);
            comm.Parameters.AddWithValue("@USERNAME", username);
            comm.Parameters.AddWithValue("@PASSWORD", password);

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

            return user;*/

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

            /*await using var conn = new SqlConnection(_connString);
            await using var comm = new SqlCommand(query, conn);
            comm.Parameters.AddWithValue("@USERNAME", username);;

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

            return user;*/

            User? user = await FetchUserAsync(query, p => p.AddWithValue("@USERNAME", username));

            return user;
        }
    }
}
