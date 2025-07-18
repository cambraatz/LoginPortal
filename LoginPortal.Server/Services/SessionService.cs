using LoginPortal.Server.Models;
using LoginPortal.Server.Services.Interfaces;
using System.Data.SqlClient;
using System.Data;
using Serilog;

namespace LoginPortal.Server.Services
{
    public class SessionService : ISessionService
    {
        private readonly string? _connString;
        private readonly ILogger<SessionService> _logger;

        public SessionService(IConfiguration config, ILogger<SessionService> logger)
        {
            _connString = config.GetConnectionString("TCS");
            _logger = logger;
        }

        public async Task<bool> AddOrUpdateSessionAsync(string username, string accessToken, string refreshToken, DateTime expiryTime, string? powerUnit, DateTime? mfstDate)
        {
            try
            {
                using (var conn = new SqlConnection(_connString))
                {
                    var checkQuery = "SELECT COUNT(1) FROM ActiveSessions WHERE Username = @Username";

                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand(checkQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@USERNAME", username);
                        var exists = (int)await cmd.ExecuteScalarAsync() > 0;
                        if (exists)
                        {
                            var updateQuery = @"
                                UPDATE dbo.SESSIONS 
                                SET ACCESSTOKEN = @ACCESSTOKEN, 
                                    REFRESHTOKEN = @REFRESHTOKEN, 
                                    EXPIRYTIME = @EXPIRYTIME, 
                                    LASTACTIVITY = @LASTACTIVITY, 
                                    POWERUNIT = @POWERUNIT, 
                                    MFSTDATE = @MFSTDATE 
                                WHERE USERNAME = @USERNAME";

                            using (var updateCmd = new SqlCommand(updateQuery, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@ACCESSTOKEN", accessToken);
                                updateCmd.Parameters.AddWithValue("@REFRESHTOKEN", refreshToken);
                                updateCmd.Parameters.AddWithValue("@EXPIRYTIME", expiryTime);
                                updateCmd.Parameters.AddWithValue("@LASTACTIVITY", DateTime.UtcNow);
                                // Handle nullable parameters for POWERUNIT and MFSTDATE
                                updateCmd.Parameters.Add("@POWERUNIT", SqlDbType.NVarChar, 50).Value = (object?)powerUnit ?? DBNull.Value;
                                updateCmd.Parameters.Add("@MFSTDATE", SqlDbType.Date).Value = (object?)mfstDate ?? DBNull.Value;
                                updateCmd.Parameters.AddWithValue("@USERNAME", username);

                                await updateCmd.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            // Insert new session
                            var insertSql = @"
                                INSERT INTO ActiveSessions (USERNAME, ACCESSTOKEN, REFRESHTOKEN, EXPIRYTIME, LOGINTIME, LASTACTIVITY, POWERUNIT, MFSTDATE)
                                VALUES (@USERNAME, @ACCESSTOKEN, @REFRESHTOKEN, @EXPIRYTIME, @LOGINTIME, @LASTACTIVITY, @POWERUNIT, @MFSTDATE)";

                            using (var insertCommand = new SqlCommand(insertSql, conn))
                            {
                                insertCommand.Parameters.AddWithValue("@USERNAME", username);
                                insertCommand.Parameters.AddWithValue("@ACCESSTOKEN", accessToken);
                                insertCommand.Parameters.AddWithValue("@REFRESHTOKEN", refreshToken);
                                insertCommand.Parameters.AddWithValue("@EXPIRYTIME", expiryTime);
                                insertCommand.Parameters.AddWithValue("@LOGINTIME", DateTime.UtcNow);
                                insertCommand.Parameters.AddWithValue("@LastActivity", DateTime.UtcNow);
                                // Handle nullable parameters for PowerUnit and MFSTDATE
                                insertCommand.Parameters.Add("@POWERUNIT", SqlDbType.NVarChar, 50).Value = (object?)powerUnit ?? DBNull.Value;
                                insertCommand.Parameters.Add("@MFSTDATE", SqlDbType.Date).Value = (object?)mfstDate ?? DBNull.Value;

                                await insertCommand.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add or update session for user {USERNAME}", username);
                return false;
            }
        }

        public async Task<SessionModel?> GetSessionAsync(string username)
        {
            try
            {
                using (var conn = new SqlConnection(_connString))
                {
                    await conn.OpenAsync();
                    var query = "SELECT ID, USERNAME, ACCESSTOKEN, REFRESHTOKEN, EXPIRYTIME, LOGINTIME, LASTACTIVITY, POWERUNIT, MFSTDATE FROM dbo.SESSIONS WHERE USERNAME = @USERNAME";
                    using (var comm = new SqlCommand(query, conn))
                    {
                        comm.Parameters.AddWithValue("@USERNAME", username);
                        using (var reader = await comm.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new SessionModel
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("ID")),
                                    Username = reader.GetString(reader.GetOrdinal("USERNAME")),
                                    AccessToken = reader.GetString(reader.GetOrdinal("ACCESSTOKEN")),
                                    RefreshToken = reader.GetString(reader.GetOrdinal("REFRESHTOKEN")),
                                    ExpiryTime = reader.GetDateTime(reader.GetOrdinal("EXPIRYTIME")),
                                    LoginTime = reader.GetDateTime(reader.GetOrdinal("LOGINTIME")),
                                    LastActivity = reader.GetDateTime(reader.GetOrdinal("LASTACTIVITY")),
                                    PowerUnit = reader.IsDBNull(reader.GetOrdinal("POWERUNIT")) ? null : reader.GetString(reader.GetOrdinal("POWERUNIT")),
                                    MfstDate = reader.IsDBNull(reader.GetOrdinal("MFSTDATE")) ? null : reader.GetDateTime(reader.GetOrdinal("MFSTDATE"))
                                };
                            }
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get session for user {Username}. Error: {Message}", username, ex.Message);
                return null;
            }
        }

        public async Task<SessionModel?> GetSessionByManifestDetailsAsync(string username, string powerUnit, DateTime mfstDate)
        {
            try 
            {
                using (var connection = new SqlConnection(_connString))
                {
                    await connection.OpenAsync();
                    // This query specifically looks for another user with the SAME PowerUnit and MfstDate
                    var sql = @"
                        SELECT TOP 1 ID, USERNAME, ACCESSTOKEN, REFRESHTOKEN, EXPIRYTIME, LOGINTIME, LASTACTIVITY, POWERUNIT, MFSTDATE
                        FROM dbo.SESSIONS
                        WHERE USERNAME != @USERNAME
                          AND POWERUNIT = @POWERUNIT
                          AND MFSTDATE = @MFSTDATE"; // Assumes non-null PowerUnit and MfstDate for conflict check
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@USERNAME", username);
                        command.Parameters.AddWithValue("@POWERUNIT", powerUnit);
                        command.Parameters.AddWithValue("@MFSTDATE", mfstDate.Date); // Ensure date comparison is consistent (e.g., just date part)

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new SessionModel
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("ID")),
                                    Username = reader.GetString(reader.GetOrdinal("USERNAME")),
                                    AccessToken = reader.GetString(reader.GetOrdinal("ACCESSTOKEN")),
                                    RefreshToken = reader.GetString(reader.GetOrdinal("REFRESHTOKEN")),
                                    ExpiryTime = reader.GetDateTime(reader.GetOrdinal("EXPIRYTIME")),
                                    LoginTime = reader.GetDateTime(reader.GetOrdinal("LOGINTIME")),
                                    LastActivity = reader.GetDateTime(reader.GetOrdinal("LASTACTIVITY")),
                                    PowerUnit = reader.IsDBNull(reader.GetOrdinal("POWERUNIT")) ? null : reader.GetString(reader.GetOrdinal("POWERUNIT")),
                                    MfstDate = reader.IsDBNull(reader.GetOrdinal("MFSTDATE")) ? null : reader.GetDateTime(reader.GetOrdinal("MFSTDATE"))
                                };
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get conflicting session for user {CurrentUsername}, PowerUnit {PU}, MfstDate {MD}. Error: {Message}", username, powerUnit, mfstDate, ex.Message);
                return null;
            }
        }

        // This method will be crucial for SSO:
        public async Task<SessionModel?> GetConflictingSessionAsync(string currentUsername, string powerUnit, DateTime mfstDate)
        {
            try
            {
                using (var connection = new SqlConnection(_connString))
                {
                    await connection.OpenAsync();
                    // This query specifically looks for another user with the SAME PowerUnit and MfstDate
                    var sql = @"
                        SELECT TOP 1 ID, USERNAME, ACCESSTOKEN, REFRESHTOKEN, EXPIRYTIME, LOGINTIME, LASTACTIVITY, POWERUNIT, MFSTDATE
                        FROM dbo.SESSIONS
                        WHERE USERNAME != @CURRUSERNAME
                          AND POWERUNIT = @POWERUNIT
                          AND MFSTDATE = @MFSTDATE"; // Assumes non-null PowerUnit and MfstDate for conflict check
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@CURRUSERNAME", currentUsername);
                        command.Parameters.AddWithValue("@POWERUNIT", powerUnit);
                        command.Parameters.AddWithValue("@MFSTDATE", mfstDate.Date); // Ensure date comparison is consistent (e.g., just date part)

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new SessionModel
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("ID")),
                                    Username = reader.GetString(reader.GetOrdinal("USERNAME")),
                                    AccessToken = reader.GetString(reader.GetOrdinal("ACCESSTOKEN")),
                                    RefreshToken = reader.GetString(reader.GetOrdinal("REFRESHTOKEN")),
                                    ExpiryTime = reader.GetDateTime(reader.GetOrdinal("EXPIRYTIME")),
                                    LoginTime = reader.GetDateTime(reader.GetOrdinal("LOGINTIME")),
                                    LastActivity = reader.GetDateTime(reader.GetOrdinal("LASTACTIVITY")),
                                    PowerUnit = reader.IsDBNull(reader.GetOrdinal("POWERUNIT")) ? null : reader.GetString(reader.GetOrdinal("POWERUNIT")),
                                    MfstDate = reader.IsDBNull(reader.GetOrdinal("MFSTDATE")) ? null : reader.GetDateTime(reader.GetOrdinal("MFSTDATE"))
                                };
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get conflicting session for user {CurrentUsername}, PowerUnit {PU}, MfstDate {MD}. Error: {Message}", currentUsername, powerUnit, mfstDate, ex.Message);
                return null;
            }
        }

        public async Task<bool> InvalidateSessionAsync(string username)
        {
            try
            {
                using (var connection = new SqlConnection(_connString))
                {
                    await connection.OpenAsync();
                    var sql = "DELETE FROM dbo.SESSIONS WHERE USERNAME = @USERNAME";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@USERNAME", username);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to invalidate session for user {Username}. Error: {Message}", username, ex.Message);
                return false;
            }
        }

        public async Task<bool> InvalidateSessionByTokensAsync(string accessToken, string refreshToken)
        {
            try
            {
                using (var connection = new SqlConnection(_connString))
                {
                    await connection.OpenAsync();
                    var sql = "DELETE FROM dbo.SESSIONS WHERE ACCESSTOKEN = @ACCESSTOKEN OR REFRESHTOKEN = @REFRESHTOKEN";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@ACCESSTOKEN", accessToken);
                        command.Parameters.AddWithValue("@REFRESHTOKEN", refreshToken);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to invalidate session by tokens. Error: {Message}", ex.Message);
                return false;
            }
        }

        public async Task CleanupExpiredSessionsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connString))
                {
                    await connection.OpenAsync();
                    var sql = "DELETE FROM dbo.SESSIONS WHERE EXPIRYTIME <= @CURRTIME";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@CURRTIME", DateTime.UtcNow);
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        _logger.LogInformation("Cleaned up {RowsAffected} expired sessions.", rowsAffected);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during expired session cleanup. Error: {Message}", ex.Message);
            }
        }

        // This method was previously in ISessionService, but might not be directly used anymore if GetConflictingSessionAsync is preferred
        /*public Task<SessionModel?> GetSessionByManifestDetailsAsync(string username, string powerUnit, DateTime mfstDate)
        {
            // This method's intent might be covered by GetConflictingSessionAsync or a direct GetSessionAsync if we're querying the user's *own* manifest.
            // If you still need this exact lookup, you'd implement it similar to GetSessionAsync but with additional WHERE clauses.
            // For now, I'll return a completed task with null as it wasn't fully defined in the EF context either.
            _logger.LogWarning("GetSessionByManifestDetailsAsync called, but its specific use case might be covered by other methods. Returning null.");
            return Task.FromResult<SessionModel?>(null);
        }*/
    }
}
