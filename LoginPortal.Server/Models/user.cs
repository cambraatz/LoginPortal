namespace LoginPortal.Server.Models
{
    public class User
    {
        public string? Username { get; set; }
        public string? Permissions {  get; set; }
        public string? Powerunit { get; set; }
        public string? ActiveCompany { get; set; }
        public List<string>? Companies { get; set; } = new List<string>();
        public List<string>? Modules { get; set; } = new List<string>();
    }
}
