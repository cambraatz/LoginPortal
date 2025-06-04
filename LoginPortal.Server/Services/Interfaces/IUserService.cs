using LoginPortal.Server.Models;

namespace LoginPortal.Server.Services.Interfaces
{
    public interface IUserService
    {
        // returns full user credentials or returns null when invalid...
        Task<User?> AuthenticateAsync(string userName, string password);

        // returns full user credentials from username along (ensure security elsewhere)...
        Task<User?> GetByUsernameAsync(string username);

        // updates existing user on new user initialization...
        Task<int> UpdateUserAsync(string username, string password, string powerunit);

        // replace 'active' company to company selected by user at login...
        Task<string?> SetCompanyAsync(string username, string company);
    }
}
