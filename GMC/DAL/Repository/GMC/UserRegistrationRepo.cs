using GMC.Models.GMC;

namespace GMC.DAL.Repository.GMC
{
    public interface IUserRegistrationRepo
    {
        bool RegisterUser(UserRegistrationModel user);
        bool RegisterUser(UserRegistrationModel user, out string errorMessage);
    }

    public class UserRegistrationRepo : IUserRegistrationRepo
    {
        private readonly UserRegistrationDAL _dal;

        public UserRegistrationRepo(UserRegistrationDAL dal)
        {
            _dal = dal;
        }

        public bool RegisterUser(UserRegistrationModel user)
        {
            return _dal.RegisterUser(user, out _);
        }

        public bool RegisterUser(UserRegistrationModel user, out string errorMessage)
        {
            return _dal.RegisterUser(user, out errorMessage);
        }
    }
}
