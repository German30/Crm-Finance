namespace CRMFinaciertoBackend.Services
{
    public record LoginResult(string Token, int ExpirationInMinutes);

    public interface IAuthService
    {
        Task<LoginResult?> LoginAsync(string email, string password);

        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}
