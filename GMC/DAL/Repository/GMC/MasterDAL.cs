using System.Data;
using System.Data.SqlClient;
using GMC.Models.GMC;

namespace GMC.DAL.Repository.GMC
{
    public class MasterDAL
    {
        private readonly IConfiguration _configuration;
        string ConnectionString = "";

        public MasterDAL(IConfiguration configuration)
        {
            _configuration = configuration;
            ConnectionString = _configuration.GetConnectionString("ConnectionToTele_Dashboard");
        }

        public List<UserRoleModel> GetUserRoles()
        {
            List<UserRoleModel> lst = new List<UserRoleModel>();
            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("SELECT RoleId, RoleName, IsActive FROM dbo.UserRoleMaster ORDER BY RoleName", con);
                    con.Open();
                    SqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        lst.Add(new UserRoleModel {
                            RoleId = Convert.ToInt32(rdr["RoleId"]),
                            RoleName = rdr["RoleName"].ToString(),
                            IsActive = Convert.ToBoolean(rdr["IsActive"])
                        });
                    }
                    con.Close();
                }
            }
            catch { }
            return lst;
        }

        public bool AddUserRole(string roleName)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("INSERT INTO dbo.UserRoleMaster (RoleName, IsActive) VALUES (@RoleName, 1)", con);
                    cmd.Parameters.AddWithValue("@RoleName", roleName);
                    con.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch { return false; }
        }

        public List<FormPermissionModel> GetPermissions()
        {
            List<FormPermissionModel> lst = new List<FormPermissionModel>();
            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    string qry = "SELECT p.PermissionId, p.RoleId, r.RoleName, p.FormName, p.CanView, p.CanEdit " +
                                 "FROM dbo.FormPermissionMaster p " +
                                 "INNER JOIN dbo.UserRoleMaster r ON p.RoleId = r.RoleId";
                    SqlCommand cmd = new SqlCommand(qry, con);
                    con.Open();
                    SqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        lst.Add(new FormPermissionModel {
                            PermissionId = Convert.ToInt32(rdr["PermissionId"]),
                            RoleId = Convert.ToInt32(rdr["RoleId"]),
                            RoleName = rdr["RoleName"].ToString(),
                            FormName = rdr["FormName"].ToString(),
                            CanView = Convert.ToBoolean(rdr["CanView"]),
                            CanEdit = Convert.ToBoolean(rdr["CanEdit"])
                        });
                    }
                    con.Close();
                }
            }
            catch { }
            return lst;
        }

        public bool AddFormPermission(FormPermissionModel perm)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("INSERT INTO dbo.FormPermissionMaster (RoleId, FormName, CanView, CanEdit) VALUES (@RoleId, @FormName, @CanView, @CanEdit)", con);
                    cmd.Parameters.AddWithValue("@RoleId", perm.RoleId);
                    cmd.Parameters.AddWithValue("@FormName", perm.FormName);
                    cmd.Parameters.AddWithValue("@CanView", perm.CanView);
                    cmd.Parameters.AddWithValue("@CanEdit", perm.CanEdit);
                    con.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch { return false; }
        }
    }
}
