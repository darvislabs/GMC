using GMC.Interface.GMC;
using GMC.Models.GMC;

namespace GMC.BL.GMC
{
    public class LoginBL : ILoginBL
    {
        private readonly ILoginRepo _repo;

        public LoginBL(ILoginRepo repo)
        {
            _repo = repo;
        }

        public async Task<bool> ValidateUser(LoginModel model)
        {
            return await _repo.ValidateUser(model);
        }

        public async Task<string?> ValidateUserAndGetRole(LoginModel model)
        {
            return await _repo.ValidateUserAndGetRole(model);
        }

        public async Task<bool> IsEmailRegistered(string email)
        {
            return await _repo.IsEmailRegistered(email);
        }

        public async Task<bool> UpdatePassword(string email, string newPassword)
        {
            return await _repo.UpdatePassword(email, newPassword);
        }
    }
}
