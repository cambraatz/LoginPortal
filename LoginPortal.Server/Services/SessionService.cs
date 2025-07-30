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

        public async Task<SessionModel?> AddOrUpdateSessionAsync(
            long userId,
            string username,
            string accessToken,
            string refreshToken,
            DateTime expiryTime,
            string? powerUnit,
            string? mfstDate)
        {
            try
            {
                string sql;
                long userId_DB = userId;
                using (var conn = new SqlConnection(_connString))
                {
                    await conn.OpenAsync();
                    if (userId == 0)
                    {
                        sql = @"INSERT INTO dbo.SESSIONS (USERNAME, ACCESSTOKEN, REFRESHTOKEN, EXPIRYTIME, LOGINTIME, LASTACTIVITY, POWERUNIT, MFSTDATE)
                        VALUES (@USERNAME, @ACCESSTOKEN, @REFRESHTOKEN, @EXPIRYTIME, @LOGINTIME, @LASTACTIVITY, @POWERUNIT, @MFSTDATE);
                        SELECT SCOPE_IDENTITY();";
                    }
                    else
                    {
                        sql = @"UPDATE dbo.SESSIONS SET 
                                USERNAME = @USERNAME,
                                ACCESSTOKEN = @ACCESSTOKEN,                             
                                REFRESHTOKEN = @REFRESHTOKEN, 
                                EXPIRYTIME = @EXPIRYTIME, 
                                LASTACTIVITY = @LASTACTIVITY, 
                                POWERUNIT = @POWERUNIT, 
                                MFSTDATE = @MFSTDATE 
                            WHERE ID = @ID;";
                    }

                    using (var command = new SqlCommand(sql, conn))
                    {
                        command.Parameters.AddWithValue("@USERNAME", username);
                        command.Parameters.AddWithValue("@ACCESSTOKEN", accessToken);
                        command.Parameters.AddWithValue("@REFRESHTOKEN", refreshToken);
                        command.Parameters.AddWithValue("@EXPIRYTIME", expiryTime);
                        command.Parameters.AddWithValue("@LASTACTIVITY", DateTime.UtcNow); // Always update last activity
                        command.Parameters.Add("@POWERUNIT", SqlDbType.NVarChar, 50).Value = (object?)powerUnit ?? DBNull.Value;
                        command.Parameters.Add("@MFSTDATE", SqlDbType.NVarChar, 8).Value = (object?)mfstDate ?? DBNull.Value;

                        if (userId == 0)
                        {
                            command.Parameters.AddWithValue("@LOGINTIME", DateTime.UtcNow); // Set login time only for new sessions
                            // ExecuteScalarAsync returns the first column of the first row (SCOPE_IDENTITY())
                            userId_DB = Convert.ToInt64(await command.ExecuteScalarAsync());
                            _logger.LogInformation("New session created for user {Username} with ID: {SessionId}", username, userId_DB);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@ID", userId); // Add ID parameter for update
                            var rowsAffected = await command.ExecuteNonQueryAsync();
                            if (rowsAffected > 0)
                            {
                                _logger.LogInformation("Session ID {SessionId} updated for user {Username}.", userId, username);
                            }
                            else
                            {
                                _logger.LogWarning("Session ID {SessionId} not found for update for user {Username}.", userId, username);
                                // If update failed (e.g., ID not found), you might want to consider it a failure.
                                return null;
                            }
                        }
                    }
                }
                return await GetSessionByIdAsync(userId_DB);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add or update session for user {Username}, ID: {UserId}. Error: {Message}", username, userId, ex.Message);
                return null;
            }
        }

        public async Task<SessionModel?> AddSessionAsync(string username, string accessToken, string refreshToken, DateTime expiryTime, string? powerUnit, string? mfstDate)
        {
            try
            {
                using (var conn = new SqlConnection(_connString))
                {
                    await conn.OpenAsync();
                    // Insert new session
                    var insertSql = @"
                        INSERT INTO dbo.SESSIONS (USERNAME, ACCESSTOKEN, REFRESHTOKEN, EXPIRYTIME, LOGINTIME, LASTACTIVITY, POWERUNIT, MFSTDATE)
                        VALUES (@USERNAME, @ACCESSTOKEN, @REFRESHTOKEN, @EXPIRYTIME, @LOGINTIME, @LASTACTIVITY, @POWERUNIT, @MFSTDATE)";

                    using (var insertCommand = new SqlCommand(insertSql, conn))
                    {
                        insertCommand.Parameters.AddWithValue("@USERNAME", username);
                        insertCommand.Parameters.AddWithValue("@ACCESSTOKEN", accessToken);
                        insertCommand.Parameters.AddWithValue("@REFRESHTOKEN", refreshToken);
                        insertCommand.Parameters.AddWithValue("@EXPIRYTIME", expiryTime);
                        insertCommand.Parameters.AddWithValue("@LOGINTIME", DateTime.UtcNow);
                        insertCommand.Parameters.AddWithValue("@LASTACTIVITY", DateTime.UtcNow);
                        // Handle nullable parameters for PowerUnit and MFSTDATE
                        insertCommand.Parameters.Add("@POWERUNIT", SqlDbType.NVarChar, 50).Value = (object?)powerUnit ?? DBNull.Value;
                        insertCommand.Parameters.Add("@MFSTDATE", SqlDbType.NVarChar, 8).Value = (object?)mfstDate ?? DBNull.Value;

                        await insertCommand.ExecuteNonQueryAsync();
                    }
                }
                return new SessionModel {
                    Username = username,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiryTime = expiryTime,
                    LoginTime = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow,
                    PowerUnit = powerUnit,
                    MfstDate = mfstDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add or update session for user {USERNAME}", username);
                Console.WriteLine($"Failed to add or update session for user {username}. Error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateSessionLastActivityByIdAsync(long sessionId)
        {
            if (string.IsNullOrEmpty(_connString))
            {
                _logger.LogError("Connection string is not configured.");
                return false;
            }
            try
            {
                using (var conn = new SqlConnection(_connString))
                {
                    await conn.OpenAsync();
                    var sql = @"
                        UPDATE dbo.SESSIONS
                        SET LASTACTIVITY = @LASTACTIVITY
                        WHERE ID = @ID";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@LASTACTIVITY", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("@ID", sessionId);
                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update session last activity for session ID {SessionId}. Error: {Message}", sessionId, ex.Message);
                return false;
            }
        }

        public async Task<bool> UpdateSessionAsync(string username, string accessToken, string refreshToken, DateTime expiryTime, string? powerUnit, string? mfstDate)
        {
            try
            {
                using (var conn = new SqlConnection(_connString))
                {
                    await conn.OpenAsync();
                    var updateQuery = @"
                        UPDATE dbo.SESSIONS 
                        SET REFRESHTOKEN = @REFRESHTOKEN, 
                            EXPIRYTIME = @EXPIRYTIME, 
                            LASTACTIVITY = @LASTACTIVITY, 
                            POWERUNIT = @POWERUNIT, 
                            MFSTDATE = @MFSTDATE 
                        WHERE USERNAME = @USERNAME AND ACCESSTOKEN = @ACCESSTOKEN";

                    using (var updateCmd = new SqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@ACCESSTOKEN", accessToken);
                        updateCmd.Parameters.AddWithValue("@REFRESHTOKEN", refreshToken);
                        updateCmd.Parameters.AddWithValue("@EXPIRYTIME", expiryTime);
                        updateCmd.Parameters.AddWithValue("@LASTACTIVITY", DateTime.UtcNow);
                        // Handle nullable parameters for POWERUNIT and MFSTDATE
                        updateCmd.Parameters.Add("@POWERUNIT", SqlDbType.NVarChar, 10).Value = (object?)powerUnit ?? DBNull.Value;
                        updateCmd.Parameters.Add("@MFSTDATE", SqlDbType.NVarChar, 8).Value = (object?)mfstDate ?? DBNull.Value;
                        updateCmd.Parameters.AddWithValue("@USERNAME", username);

                        await updateCmd.ExecuteNonQueryAsync();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add or update session for user {USERNAME}", username);
                Console.WriteLine($"Failed to add or update session for user {username}. Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateSessionLastActivityAsync(string username, string accessToken)
        {
            if (string.IsNullOrEmpty(_connString))
            {
                _logger.LogError("Connection string is not configured.");
                return false;
            }

            try
            {
                using (var conn = new SqlConnection(_connString))
                {
                    await conn.OpenAsync();
                    var sql = @"
                        UPDATE dbo.SESSIONS
                        SET LASTACTIVITY = @LASTACTIVITY
                        WHERE USERNAME = @USERNAME AND ACCESSTOKEN = @ACCESSTOKEN";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@LASTACTIVITY", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("@USERNAME", username);
                        cmd.Parameters.AddWithValue("@ACCESSTOKEN", accessToken);
                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update session last activity for user {Username}. Error: {Message}", username, ex.Message);
                return false;
            }
        }

        public async Task<SessionModel?> GetSessionByIdAsync(long userId)
        {
            if (string.IsNullOrEmpty(_connString))
            {
                _logger.LogError("Connection string is not configured.");
                return null;
            }
            try
            {
                using (var conn = new SqlConnection(_connString))
                {
                    await conn.OpenAsync();
                    var query = @"
                        SELECT ID, USERNAME, ACCESSTOKEN, REFRESHTOKEN, EXPIRYTIME, LOGINTIME, LASTACTIVITY, POWERUNIT, MFSTDATE 
                        FROM dbo.SESSIONS WHERE ID = @ID";
                    using (var comm = new SqlCommand(query, conn))
                    {
                        comm.Parameters.AddWithValue("@ID", userId);
                        using (var reader = await comm.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new SessionModel
                                {
                                    Id = reader.GetInt64(reader.GetOrdinal("ID")),
                                    Username = reader.GetString(reader.GetOrdinal("USERNAME")),
                                    AccessToken = reader.GetString(reader.GetOrdinal("ACCESSTOKEN")),
                                    RefreshToken = reader.GetString(reader.GetOrdinal("REFRESHTOKEN")),
                                    ExpiryTime = reader.GetDateTime(reader.GetOrdinal("EXPIRYTIME")),
                                    LoginTime = reader.GetDateTime(reader.GetOrdinal("LOGINTIME")),
                                    LastActivity = reader.GetDateTime(reader.GetOrdinal("LASTACTIVITY")),
                                    PowerUnit = reader.IsDBNull(reader.GetOrdinal("POWERUNIT")) ? null : reader.GetString(reader.GetOrdinal("POWERUNIT")),
                                    MfstDate = reader.IsDBNull(reader.GetOrdinal("MFSTDATE")) ? null : reader.GetString(reader.GetOrdinal("MFSTDATE"))
                                };
                            }
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get session by ID {UserId}. Error: {Message}", userId, ex.Message);
                return null;
            }
        }

        public async Task<SessionModel?> GetSessionAsync(string username, string accessToken, string refreshToken)
        {
            try
            {
                using (var conn = new SqlConnection(_connString))
                {
                    await conn.OpenAsync();
                    var query = @"
                        SELECT ID, USERNAME, ACCESSTOKEN, REFRESHTOKEN, EXPIRYTIME, LOGINTIME, LASTACTIVITY, POWERUNIT, MFSTDATE 
                        FROM dbo.SESSIONS WHERE USERNAME = @USERNAME AND (ACCESSTOKEN = @ACCESSTOKEN OR REFRESHTOKEN = @REFRESHTOKEN)";
                    using (var comm = new SqlCommand(query, conn))
                    {
                        comm.Parameters.AddWithValue("@USERNAME", username);
                        comm.Parameters.AddWithValue("@ACCESSTOKEN", accessToken);
                        comm.Parameters.AddWithValue("@REFRESHTOKEN", refreshToken);

                        using (var reader = await comm.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new SessionModel
                                {
                                    Id = reader.GetInt64(reader.GetOrdinal("ID")),
                                    Username = reader.GetString(reader.GetOrdinal("USERNAME")),
                                    AccessToken = reader.GetString(reader.GetOrdinal("ACCESSTOKEN")),
                                    RefreshToken = reader.GetString(reader.GetOrdinal("REFRESHTOKEN")),
                                    ExpiryTime = reader.GetDateTime(reader.GetOrdinal("EXPIRYTIME")),
                                    LoginTime = reader.GetDateTime(reader.GetOrdinal("LOGINTIME")),
                                    LastActivity = reader.GetDateTime(reader.GetOrdinal("LASTACTIVITY")),
                                    PowerUnit = reader.IsDBNull(reader.GetOrdinal("POWERUNIT")) ? null : reader.GetString(reader.GetOrdinal("POWERUNIT")),
                                    MfstDate = reader.IsDBNull(reader.GetOrdinal("MFSTDATE")) ? null : reader.GetString(reader.GetOrdinal("MFSTDATE"))
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

        public async Task<SessionModel?> GetSessionByManifestDetailsAsync(string username, string powerUnit, string mfstDate, string accessToken, string refreshToken)
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
                        WHERE ACCESSTOKEN != @ACCESSTOKEN 
                          OR REFRESHTOKEN != @REFRESHTOKEN
                          AND POWERUNIT = @POWERUNIT
                          AND MFSTDATE = @MFSTDATE"; // Assumes non-null PowerUnit and MfstDate for conflict check
                    using (var command = new SqlCommand(sql, connection))
                    {
                        //command.Parameters.AddWithValue("@USERNAME", username);
                        command.Parameters.AddWithValue("@POWERUNIT", powerUnit);
                        command.Parameters.AddWithValue("@MFSTDATE", mfstDate); // Ensure date comparison is consistent (e.g., just date part)
                        command.Parameters.AddWithValue("@ACCESSTOKEN", accessToken);
                        command.Parameters.AddWithValue("@REFRESHTOKEN", refreshToken);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new SessionModel
                                {
                                    Id = reader.GetInt64(reader.GetOrdinal("ID")),
                                    Username = reader.GetString(reader.GetOrdinal("USERNAME")),
                                    AccessToken = reader.GetString(reader.GetOrdinal("ACCESSTOKEN")),
                                    RefreshToken = reader.GetString(reader.GetOrdinal("REFRESHTOKEN")),
                                    ExpiryTime = reader.GetDateTime(reader.GetOrdinal("EXPIRYTIME")),
                                    LoginTime = reader.GetDateTime(reader.GetOrdinal("LOGINTIME")),
                                    LastActivity = reader.GetDateTime(reader.GetOrdinal("LASTACTIVITY")),
                                    PowerUnit = reader.IsDBNull(reader.GetOrdinal("POWERUNIT")) ? null : reader.GetString(reader.GetOrdinal("POWERUNIT")),
                                    MfstDate = reader.IsDBNull(reader.GetOrdinal("MFSTDATE")) ? null : reader.GetString(reader.GetOrdinal("MFSTDATE"))
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
        public async Task<SessionModel?> GetConflictingSessionAsync(string currUsername, string powerUnit, string mfstDate, string accessToken, string refreshToken)
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
                        WHERE 
                            POWERUNIT COLLATE SQL_Latin1_General_CP1_CI_AS = @POWERUNIT COLLATE SQL_Latin1_General_CP1_CI_AS
                            AND MFSTDATE COLLATE SQL_Latin1_General_CP1_CI_AS = @MFSTDATE COLLATE SQL_Latin1_General_CP1_CI_AS
                            AND ACCESSTOKEN != @ACCESSTOKEN"; // Assumes non-null PowerUnit and MfstDate for conflict check

                    using (var command = new SqlCommand(sql, connection))
                    {
                        //command.Parameters.AddWithValue("@CURRUSERNAME", currUsername);
                        command.Parameters.Add("@MFSTDATE", SqlDbType.NVarChar, 8).Value = mfstDate.Trim();
                        command.Parameters.Add("@POWERUNIT", SqlDbType.NVarChar, 10).Value = powerUnit.Trim();
                        command.Parameters.Add("@ACCESSTOKEN", SqlDbType.NVarChar, -1).Value = accessToken.Trim();
                        //command.Parameters.AddWithValue("@REFRESHTOKEN", refreshToken);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new SessionModel
                                {
                                    Id = reader.GetInt64(reader.GetOrdinal("ID")),
                                    Username = reader.GetString(reader.GetOrdinal("USERNAME")),
                                    AccessToken = reader.GetString(reader.GetOrdinal("ACCESSTOKEN")),
                                    RefreshToken = reader.GetString(reader.GetOrdinal("REFRESHTOKEN")),
                                    ExpiryTime = reader.GetDateTime(reader.GetOrdinal("EXPIRYTIME")),
                                    LoginTime = reader.GetDateTime(reader.GetOrdinal("LOGINTIME")),
                                    LastActivity = reader.GetDateTime(reader.GetOrdinal("LASTACTIVITY")),
                                    PowerUnit = reader.IsDBNull(reader.GetOrdinal("POWERUNIT")) ? null : reader.GetString(reader.GetOrdinal("POWERUNIT")),
                                    MfstDate = reader.IsDBNull(reader.GetOrdinal("MFSTDATE")) ? null : reader.GetString(reader.GetOrdinal("MFSTDATE"))
                                };
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get conflicting session for user {currUsername}, PowerUnit {PU}, MfstDate {MD}. Error: {Message}", currUsername, powerUnit, mfstDate, ex.Message);
                Console.WriteLine($"Failed to get conflicting session for user {currUsername}, PowerUnit {powerUnit}, MfstDate {mfstDate}. Error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteSessionByIdAsync(long sessionId)
        {
            try
            {
                using (var connection = new SqlConnection(_connString))
                {
                    await connection.OpenAsync();
                    var sql = "DELETE FROM dbo.SESSIONS WHERE ID = @ID";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@ID", sessionId);
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete session with ID {SessionId}. Error: {Message}", sessionId, ex.Message);
                return false;
            }
        }

        public async Task<bool> InvalidateSessionAsync(string username, string accessToken, string refreshToken)
        {
            try
            {
                using (var connection = new SqlConnection(_connString))
                {
                    await connection.OpenAsync();
                    var sql = "DELETE FROM dbo.SESSIONS WHERE USERNAME = @USERNAME AND ACCESSTOKEN = @ACCESSTOKEN AND REFRESHTOKEN != @REFRESHTOKEN";
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

        public async Task<bool> InvalidateSessionByDeliveryManifest(string username, string powerunit, string mfstdate)
        {
            try
            {
                using (var connection = new SqlConnection(_connString))
                {
                    await connection.OpenAsync();
                    var sql = "DELETE FROM dbo.SESSIONS WHERE USERNAME = @USERNAME AND POWERUNIT = @POWERUNIT AND MFSTDATE = @MFSTDATE";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@USERNAME", username);
                        command.Parameters.AddWithValue("@POWERUNIT", powerunit);
                        command.Parameters.AddWithValue("@MFSTDATE", mfstdate);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to invalidate session by manifest credentials. Error: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<bool> ResetSessionByDeliveryManifestAsync(string username, string powerunit, string mfstdate, string accessToken)
        {
            try
            {
                using (var connection = new SqlConnection(_connString))
                {
                    await connection.OpenAsync();
                    var sql = "UPDATE dbo.SESSIONS SET POWERUNIT = NULL, MFSTDATE = NULL WHERE USERNAME = @USERNAME AND ACCESSTOKEN = @ACCESSTOKEN";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@USERNAME", username);
                        command.Parameters.AddWithValue("@POWERUNIT", powerunit);
                        command.Parameters.AddWithValue("@MFSTDATE", mfstdate);
                        command.Parameters.AddWithValue("@ACCESSTOKEN", accessToken);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to invalidate session by manifest credentials. Error: {Message}", ex.Message);
                return false;
            }
        }

        public async Task CleanupExpiredSessionsAsync(TimeSpan idleTimeout)
        {
            try
            {
                using (var connection = new SqlConnection(_connString))
                {
                    await connection.OpenAsync();
                    var now = DateTime.UtcNow;
                    var idleThreshold = now.Subtract(idleTimeout);
                    var sql = @"
                        DELETE FROM dbo.SESSIONS WHERE EXPIRYTIME <= @CURRTIME
                        OR (LASTACTIVITY <= @CURRTIME AND LASTACTIVITY < @IDLETHRESHOLD)        
                    ";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@CURRTIME", DateTime.UtcNow);
                        command.Parameters.AddWithValue("@IDLETHRESHOLD", idleThreshold);
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
