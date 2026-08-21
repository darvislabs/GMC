using GMC.Models.GMC;

namespace GMC.Interface.GMC
{
    public interface ILoginBL
    {
        Task<bool> ValidateUser(LoginModel model);
        Task<string?> ValidateUserAndGetRole(LoginModel model);
        Task<bool> IsEmailRegistered(string email);
        Task<bool> UpdatePassword(string email, string newPassword);
    }
}
