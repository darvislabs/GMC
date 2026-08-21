using GMC.Interface.GMC;
using GMC.Models.GMC;

namespace GMC.DAL.Repository.GMC
{
    public class LoginRepo : ILoginRepo
    {
        private readonly LoginDAL _dal;

        public LoginRepo(LoginDAL dal)
        {
            _dal = dal;
        }

        public async Task<bool> ValidateUser(LoginModel model)
        {
            return await _dal.ValidateUser(model);
        }

        public async Task<string?> ValidateUserAndGetRole(LoginModel model)
        {
            return await _dal.ValidateUserAndGetRole(model);
        }

        public async Task<bool> IsEmailRegistered(string email)
        {
            return await _dal.IsEmailRegistered(email);
        }

        public async Task<bool> UpdatePassword(string email, string newPassword)
        {
            return await _dal.UpdatePassword(email, newPassword);
        }
    }
}
