using System.Data;
using System.Data.SqlClient;
using GMC.Models.GMC;

namespace GMC.DAL.Repository.GMC
{
    public class GMCUploaderDAL
    {
        readonly IConfiguration _config;
        string conn = "";
        readonly IHttpContextAccessor _httpContext;
        string userName = "";
        
        
        public GMCUploaderDAL(IConfiguration config, IHttpContextAccessor httpContext)
        {
            _config = config;
            conn = _config["ConnectionStrings:ConnectionToTele_Dashboard"];
            _httpContext = httpContext;
            //userName = _httpContext.HttpContext.User.FindFirst("UserName").Value;
        }

        public async Task<int> insert_renewal_enrollmentdata(GMCUploaderModel model)
        {
            SqlConnection connDash = new SqlConnection(conn);
            SqlCommand cmd = new SqlCommand();
            try
            {
                await connDash.OpenAsync();
                cmd.CommandText = "SP_insert_renewal_enrollmentdata";
                //cmd.Connection = conToDPR;
                cmd.Connection = connDash;
                string ss = model.strPolicyNo.TrimEnd().TrimStart();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@policyno", SqlDbType.VarChar).Value = model.strPolicyNo.TrimEnd().TrimStart();
                cmd.Parameters.Add("@Created_By", SqlDbType.VarChar).Value = userName;
                cmd.Parameters.Add("@Industry_Name", SqlDbType.VarChar).Value = model.InsuranceCompanyName==null?"": model.InsuranceCompanyName;
                cmd.Parameters.Add("@Sub_type", SqlDbType.VarChar).Value = Enum.GetName(typeof(subType), int.Parse(model.subType));
                int status = cmd.ExecuteNonQuery();
                return status;

            }
            catch (Exception ex)
            {

                throw;
            }
            finally
            {
               
                await connDash.CloseAsync();
                connDash.Dispose();
            }
        }
        public async Task<DataSet> GetGMCParameter(string parameter, string typeofdata)
        {
            SqlConnection connDash = new SqlConnection(conn);
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            try
            {
                if (typeofdata == "Claim")
                { cmd.CommandText = "udsp_GMS_Column_Plotting"; }
                else { cmd.CommandText = "udsp_GMS_Column_Plotting_enrollment"; }

                cmd.Parameters.Add("@string", SqlDbType.VarChar).Value = (parameter);
                cmd.Parameters.Add("@typeofData", SqlDbType.VarChar).Value = (typeofdata);
                cmd.Connection = connDash;//conn;
               await connDash.OpenAsync();
                cmd.CommandType = CommandType.StoredProcedure;
                adapter = new SqlDataAdapter(cmd);
                adapter.SelectCommand.CommandTimeout = 0;
                adapter.Fill(ds1);
                return ds1;
            }
            catch (Exception ex)
            {
                throw;
               await connDash.CloseAsync();
                connDash.Dispose();
            }
            finally
            {
                ds1.Dispose();
                adapter.Dispose();
                await connDash.CloseAsync();
                connDash.Dispose();
            }
        }

        public DataSet GetGMCPendingClaimStatus(string parameter)
        {
            SqlConnection connDash = new SqlConnection(conn);
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            try
            {
                cmd.CommandText = "udsp_GMS_Column_ForClaimStatusMapping";
                cmd.Parameters.AddWithValue("@string", parameter);
                cmd.Connection = connDash;//conn;
                connDash.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                adapter = new SqlDataAdapter(cmd);
                adapter.SelectCommand.CommandTimeout = 0;
                adapter.Fill(ds1);
                return ds1;
            }
            catch (Exception ex)
            {
                throw;
                connDash.Close();
                connDash.Dispose();
            }
            finally
            {
                ds1.Dispose();
                adapter.Dispose();
                connDash.Close();
                connDash.Dispose();
            }
        }
        public void truncateTable(string TypeofData, string PoNO)
        {

            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            SqlConnection conn_sales = new SqlConnection(conn);
            if (TypeofData == "Claim")
            { cmd.CommandText = "Sp_delete_Claim_data"; }
            else
            {
                cmd.CommandText = "Sp_delete_Enrollment_data";
            }
            cmd.Parameters.Add("@policyno", SqlDbType.VarChar).Value = PoNO;

            conn_sales.Open();
            cmd.Connection = conn_sales;//conn;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.ExecuteNonQuery();

        }
        public void insert_log(string TypeofData, string PoNO)
        {

            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            SqlConnection conn_sales = new SqlConnection(conn);
            if (TypeofData == "Claim")
            { cmd.CommandText = "Sp_insert_Claim_log"; }
            else
            {
                cmd.CommandText = "Sp_insert_Enrollment_log";
            }
            cmd.Parameters.Add("@policyno", SqlDbType.VarChar).Value = PoNO;
            conn_sales.Open();
            cmd.Connection = conn_sales;//conn;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.ExecuteNonQuery();

        }
        public string insertdata(GMCUploaderModel model,string columns,string values,DataTable dt3)
        {
            string sqlCommandInsert = "";
            string msg = "";
            if (model.typeofData == "Claim")
            {
                //insert_log("Claim", txtPolicyNo.Text);
                //truncateTable("Claim", txtPolicyNo.Text);
                sqlCommandInsert = string.Format("INSERT INTO dbo.tbl_GMC_Claim_Data_new({0}) VALUES ({1})", columns, values);
                msg = "Claim data save successfully............";
            }
            else
            {
                //insert_log("endroll", txtPolicyNo.Text);
                //truncateTable("endroll", txtPolicyNo.Text);
                sqlCommandInsert = string.Format("INSERT INTO dbo.tbl_GMC_Enrollment_Data({0}) VALUES ({1})", columns, values);
                msg = "Enrollment data save successfully............";

            }
            int inserted = 0;
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            SqlConnection conn_sales = new SqlConnection(conn);

            using (var cmd1 = new SqlCommand(sqlCommandInsert, conn_sales))
            {
                conn_sales.Open();
                foreach (DataRow row in dt3.Rows)
                {
                    cmd1.Parameters.Clear();
                    foreach (DataColumn col in dt3.Columns)
                        cmd1.Parameters.AddWithValue("@" + col.ColumnName, row[col]);
                    inserted = cmd1.ExecuteNonQuery();
                }
            }
            return msg;
        }
        public int checkVersionList(string txtPolicyNo)
        {
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataTable dt = new DataTable();
            SqlCommand cmd = new SqlCommand();
            SqlConnection conn_sales = new SqlConnection(conn);



            try
            {


                cmd.CommandText = "SP_GMC_version_list";
                cmd.Connection = conn_sales;//conn;
                conn_sales.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@policyno", txtPolicyNo.Trim());
                adapter = new SqlDataAdapter(cmd);
                adapter.SelectCommand.CommandTimeout = 100;
                adapter.Fill(dt);
                int count = dt.Rows.Count;
                return count;


            }
            catch (Exception ex)
            {
                throw;
                conn_sales.Close();
                conn_sales.Dispose();
            }
            finally
            {

                adapter.Dispose();
                conn_sales.Close();
                conn_sales.Dispose();
            }
        }
        public void Update_SumInsured(string txtPolicyNo)
        {
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            SqlConnection conn_sales = new SqlConnection(conn);



            try
            {


                cmd.CommandText = "SP_Update_SumInsured";
                cmd.Connection = conn_sales;//conn;
                conn_sales.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@PolicyNO", SqlDbType.VarChar).Value = txtPolicyNo;
                cmd.CommandTimeout = 3600;
                cmd.ExecuteNonQuery();


            }
            catch (Exception ex)
            {
                throw;
                conn_sales.Close();
                conn_sales.Dispose();
            }
            finally
            {
                ds1.Dispose();
                adapter.Dispose();
                conn_sales.Close();
                conn_sales.Dispose();
            }
        }
        public async Task<int> UpdateMaster(string typeofData,string MasterParameter,string tnames)
        {
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            SqlConnection conn_sales = new SqlConnection(conn);



            try
            {
                if (tnames== "Claim")
                {
                    cmd = new SqlCommand("udsp_Save_GMC_Claim_MappingDatta", conn_sales);
                }
                else
                {
                    if (typeofData == "Claim")
                    {
                        cmd = new SqlCommand("udsp_Save_GMC_Claim_MappingDatta", conn_sales);
                    }
                    else
                    {
                        cmd = new SqlCommand("udsp_Save_GMC_Enrollment_MappingDatta", conn_sales);
                    }
                }
                

                //cmd.CommandText = "SP_Update_SumInsured";
                cmd.Connection = conn_sales;//conn;
               await conn_sales.OpenAsync();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@MasterParameter", SqlDbType.VarChar).Value = MasterParameter;
                cmd.CommandTimeout = 3600;
               int result= cmd.ExecuteNonQuery();
                return result;


            }
            catch (Exception ex)
            {
                throw;
               conn_sales.CloseAsync();
                conn_sales.Dispose();
            }
            finally
            {
                ds1.Dispose();
                adapter.Dispose();
                await conn_sales.CloseAsync();
                conn_sales.Dispose();
            }
        }
    }
}
