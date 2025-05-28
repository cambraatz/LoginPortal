namespace LoginPortal.Server.Services.Interfaces
{
    public interface IMappingService
    {
        Task<IDictionary<string, string>> GetCompaniesAsync();
        Task<IDictionary<string, string>> GetModulesAsync();
    }
}
