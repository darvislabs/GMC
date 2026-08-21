using GMC.DAL.Repository.GMC;
using GMC.Models.GMC;

namespace GMC.Models.GMC.BusinessLogic
{
    public interface IUserRegistrationBL
    {
        bool RegisterUser(UserRegistrationModel user);
        bool RegisterUser(UserRegistrationModel user, out string errorMessage);
    }

    public class UserRegistrationBL : IUserRegistrationBL
    {
        private readonly IUserRegistrationRepo _repo;

        public UserRegistrationBL(IUserRegistrationRepo repo)
        {
            _repo = repo;
        }

        public bool RegisterUser(UserRegistrationModel user)
        {
            return _repo.RegisterUser(user, out _);
        }

        public bool RegisterUser(UserRegistrationModel user, out string errorMessage)
        {
            return _repo.RegisterUser(user, out errorMessage);
        }
    }
}
