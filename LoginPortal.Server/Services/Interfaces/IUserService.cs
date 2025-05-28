using LoginPortal.Server.Models;

namespace LoginPortal.Server.Services.Interfaces
{
    public interface IUserService
    {
        // returns full user credentials or returns null when invalid...
        Task<User?> AuthenticateAsync(string userName, string password);

        // returns full user credentials from username along (ensure security elsewhere)...
        Task<User?> GetByUsernameAsync(string username);
    }
}
