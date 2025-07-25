﻿namespace LoginPortal.Server.Models
{
    public class SessionModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? PowerUnit { get; set; }
        public DateTime? MfstDate { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiryTime { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime LastActivity { get; set; }
    }
}
