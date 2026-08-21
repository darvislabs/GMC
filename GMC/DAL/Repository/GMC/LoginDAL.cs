using System.Data;
using System.Data.SqlClient;
using GMC.Models.GMC;

namespace GMC.DAL.Repository.GMC
{
    public class LoginDAL
    {
        readonly IConfiguration _config;
        string conn = "";

        public LoginDAL(IConfiguration config)
        {
            _config = config;
            conn = _config["ConnectionStrings:ConnectionToTele_Dashboard"];
        }

        public async Task<bool> ValidateUser(LoginModel model)
        {
            return !string.IsNullOrEmpty(await ValidateUserAndGetRole(model));
        }

        public async Task<string?> ValidateUserAndGetRole(LoginModel model)
        {
            using (SqlConnection connDash = new SqlConnection(conn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    try
                    {
                        await connDash.OpenAsync();
                        cmd.CommandText = "SELECT TOP 1 usertype FROM UserRegistration WHERE Username = @Username AND Password = @Password";
                        cmd.Connection = connDash;
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.Add("@Username", SqlDbType.VarChar).Value = model.Username ?? string.Empty;
                        cmd.Parameters.Add("@Password", SqlDbType.VarChar).Value = model.Password ?? string.Empty;

                        var role = await cmd.ExecuteScalarAsync();
                        return role == null || role == DBNull.Value ? null : role.ToString();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }
        public async Task<bool> IsEmailRegistered(string email)
        {
            using (SqlConnection connDash = new SqlConnection(conn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    try
                    {
                        await connDash.OpenAsync();
                        cmd.CommandText = "SELECT COUNT(*) FROM UserRegistration WHERE emailid = @Email";
                        cmd.Connection = connDash;
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.Add("@Email", SqlDbType.VarChar).Value = email ?? string.Empty;
                        
                        int count = (int)await cmd.ExecuteScalarAsync();
                        return count > 0;
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }

        public async Task<bool> UpdatePassword(string email, string newPassword)
        {
            using (SqlConnection connDash = new SqlConnection(conn))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    try
                    {
                        await connDash.OpenAsync();
                        cmd.CommandText = "UPDATE UserRegistration SET Password = @Password WHERE emailid = @Email";
                        cmd.Connection = connDash;
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.Add("@Password", SqlDbType.VarChar).Value = newPassword ?? string.Empty;
                        cmd.Parameters.Add("@Email", SqlDbType.VarChar).Value = email ?? string.Empty;
                        
                        int rows = await cmd.ExecuteNonQueryAsync();
                        return rows > 0;
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }
    }
}
