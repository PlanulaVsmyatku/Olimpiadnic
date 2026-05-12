namespace Olimpiadnic.Services
{
    public interface IPasswordRecoveryService
    {
        Task<PasswordRecoveryResult> SendResetLinkAsync(string email, string scheme, string host);
        Task<PasswordRecoveryResult> ResetPasswordAsync(string email, string token, string newPassword);
    }

    public class PasswordRecoveryResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string ResetLink { get; set; }
    }
}