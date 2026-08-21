using System.Data;
using System.Data.SqlClient;
using GMC.Models.GMC;

namespace GMC.DAL.Repository.GMC
{
    public class UserRegistrationDAL
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserRegistrationDAL> _log;
        private readonly string _connectionString;

        public UserRegistrationDAL(IConfiguration configuration, ILogger<UserRegistrationDAL> log)
        {
            _configuration = configuration;
            _log           = log;
            _connectionString = _configuration.GetConnectionString("ConnectionToTele_Dashboard")
                ?? throw new InvalidOperationException("Connection string 'ConnectionToTele_Dashboard' is missing.");
        }

        /// <summary>
        /// Inserts a user via <c>SP_InsertUserRegistration</c>.  The SP uses
        /// <c>SET NOCOUNT ON</c> and returns the new identity via
        /// <c>SELECT SCOPE_IDENTITY()</c>, so we MUST use ExecuteScalar — not
        /// ExecuteNonQuery (which would return -1 and falsely look like failure).
        ///
        /// On failure <paramref name="errorMessage"/> is populated with a
        /// user-friendly reason; the underlying exception is also logged.
        /// </summary>
        public bool RegisterUser(UserRegistrationModel user, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using var con = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("SP_InsertUserRegistration", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Username",  (object?)user.Username ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Password",  (object?)user.Password ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Name",      (object?)user.Name     ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@emailid",   (object?)user.emailid  ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@mobile",    (object?)user.mobile   ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@address",   (object?)user.address  ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@usertype",  (object?)user.usertype ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@createdby", (object?)(user.createdby ?? "System") ?? DBNull.Value);

                con.Open();
                var newId = cmd.ExecuteScalar();
                con.Close();

                if (newId == null || newId == DBNull.Value) return false;
                return Convert.ToInt32(newId) > 0;
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                // 2627 / 2601 = unique constraint / index violation
                errorMessage = $"That username (\"{user.Username}\") is already in use.";
                _log.LogWarning(ex, "Duplicate username on registration: {Username}", user.Username);
                return false;
            }
            catch (SqlException ex) when (ex.Number == 2812)
            {
                errorMessage = "Stored procedure 'SP_InsertUserRegistration' was not found on the configured database. "
                             + "Run UserRegistrationScript.sql against the GMC database.";
                _log.LogError(ex, "Missing SP — UserRegistration.");
                return false;
            }
            catch (SqlException ex) when (ex.Number == 207 || ex.Number == 208)
            {
                errorMessage = "Database object referenced by SP_InsertUserRegistration is missing. "
                             + "Check that dbo.UserRegistration exists in the GMC database.";
                _log.LogError(ex, "Missing table/column — UserRegistration.");
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = "Registration failed: " + ex.Message;
                _log.LogError(ex, "UserRegistration insert failed for {Username}", user.Username);
                return false;
            }
        }

        // Back-compat shim — keeps any older callers compiling without forcing
        // them to read the error message.  Existing IUserRegistrationRepo
        // interface (defined inside UserRegistrationRepo.cs) calls this one.
        public bool RegisterUser(UserRegistrationModel user) => RegisterUser(user, out _);
    }
}
