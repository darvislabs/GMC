using System.Data.SqlClient;
using System.Data;
using iTextSharp.text;
using GMC.Models.GMC;
using NuGet.Common;
using System.Security.Cryptography;

namespace GMC.DAL.Repository.GMC
{
    public class GMCCalculatorDetailsDAL
    {
        readonly IConfiguration _configuration;
        readonly IHttpContextAccessor _httpContext;
        string userName = "";
        string userEName = "";
        public GMCCalculatorDetailsDAL(IConfiguration configuration, IHttpContextAccessor httpContext)
        {
            _configuration = configuration;
            _httpContext = httpContext;
            //userName = _httpContext.HttpContext.User.FindFirst("UserName").Value;
            //userEName = _httpContext.HttpContext.User.FindFirst("EmpDesg").Value;
        }
        public async Task<DataTable> policyPendingDetails(string type)
        {
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            SqlConnection conn_sales = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());


            decimal startRejection = 0;
            decimal IBNR = 0;
            try
            {


                cmd.CommandText = "udsp_GetQuotePendingData";
                cmd.Parameters.Add("@Type", SqlDbType.VarChar).Value = type;
                cmd.Connection = conn_sales;//conn;
               await conn_sales.OpenAsync();
                cmd.CommandType = CommandType.StoredProcedure;
                adapter = new SqlDataAdapter(cmd);
                adapter.SelectCommand.CommandTimeout = 0;
                adapter.Fill(ds1);
                DataTable dt = new DataTable();
                if (ds1.Tables[0].Rows.Count > 0)
                {
                    
                    DataTable dtloading = new DataTable();
                    DataTable dtloadingFactor = new DataTable();
                    dt = ds1.Tables[0];
                 

                }
                return dt;

            }
            catch (Exception ex)
            {
                
                await conn_sales.CloseAsync();
                conn_sales.Dispose();
                throw;
            }
            finally
            {
                ds1.Dispose();
                adapter.Dispose();
               await conn_sales.CloseAsync();
                conn_sales.Dispose();
            }
        }
        public async Task<DataSet> UpdateGridValue_new_lives(string Factor, DataTable DT, decimal BurnAmtPremium, decimal Enrollmentpremium, string policyno)
        {
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();

            SqlConnection conn_sales = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());
            try
            {
                cmd.CommandText = "Udsp_GetFactor_LoadingAndDiscount_Rate_Rollover";
                cmd.Parameters.Add("@factor", SqlDbType.VarChar, (500)).Value = Factor;

                cmd.Parameters.Add("@BurnAmtPremium", SqlDbType.Decimal).Value = BurnAmtPremium;
                cmd.Parameters.Add("@Enrollmentpremium", SqlDbType.Decimal).Value = Enrollmentpremium;
                cmd.Parameters.Add("@policyno", SqlDbType.Decimal).Value = policyno;

                cmd.Connection = conn_sales;//conn;
               await conn_sales.OpenAsync();
                cmd.CommandType = CommandType.StoredProcedure;
                adapter = new SqlDataAdapter(cmd);
                adapter.SelectCommand.CommandTimeout = 0;
                adapter.Fill(ds1);
                return ds1;
            }

            catch (Exception ex)
            {
                throw;
               await conn_sales.CloseAsync();
                conn_sales.Dispose();
            }
            finally
            {
                await conn_sales.CloseAsync();
                conn_sales.Dispose();
            }

        }
        public async Task<DataSet> UpdateGridValue_new(string Factor, DataTable DT, decimal BurnAmtPremium, decimal Enrollmentpremium,string policyno)
        {
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();

            SqlConnection conn_sales = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());
            try
            {
                cmd.CommandText = "Udsp_GetFactor_LoadingAndDiscount_Rate_new";
                cmd.Parameters.Add("@factor", SqlDbType.VarChar, (500)).Value = Factor;

                cmd.Parameters.Add("@BurnAmtPremium", SqlDbType.Decimal).Value = BurnAmtPremium;
                cmd.Parameters.Add("@Enrollmentpremium", SqlDbType.Decimal).Value = Enrollmentpremium;
                cmd.Parameters.Add("@policyno", SqlDbType.Decimal).Value = policyno;

                cmd.Connection = conn_sales;//conn;
               await conn_sales.OpenAsync();
                cmd.CommandType = CommandType.StoredProcedure;
                adapter = new SqlDataAdapter(cmd);
                adapter.SelectCommand.CommandTimeout = 0;
                adapter.Fill(ds1);
                return ds1;
            }

            catch (Exception ex)
            {
                throw;
               await conn_sales.CloseAsync();
                conn_sales.Dispose();
            }
            finally
            {
                await conn_sales.CloseAsync();
                conn_sales.Dispose();
            }

        }
        public async Task<int> uploadXlsFile(DataTable dt,string PolicyNo)
        {
            SqlConnection conn_sales = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());
            try
            {
                
                SqlCommand command;
                command = new SqlCommand("Sp_GMC_rollover_SI", conn_sales);
                await conn_sales.OpenAsync();
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@udt_rollover_SI", dt));
                command.Parameters.Add(new SqlParameter("@Policyno", PolicyNo));
                return await command.ExecuteNonQueryAsync();
            }
            catch (Exception)
            {
                await conn_sales.CloseAsync();
                throw;
            }
           
        }
        public async Task<int> uploadXlsFile2(DataTable dt, string PolicyNo)
        {
            SqlConnection conn_sales = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());
            try
            {

                SqlCommand command;
                command = new SqlCommand("Sp_GMC_rollover_lives", conn_sales);
                await conn_sales.OpenAsync();
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@udt_rollover_lives", dt));
                command.Parameters.Add(new SqlParameter("@Policyno", PolicyNo));
                return await command.ExecuteNonQueryAsync();
            }
            catch (Exception)
            {
                await conn_sales.CloseAsync();
                throw;
            }

        }
        public async Task<int> InsertBurnCostData(string PertiCular, decimal TotalAmount, decimal NoOfClaim, decimal Acs, string versionNo)
        {
            try
            {
                 SqlConnection conn_sales_new = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds1 = new DataSet();
                SqlCommand cmd = new SqlCommand();
                try
                {

                   await conn_sales_new.OpenAsync();
                    cmd = new SqlCommand("udsp_insertGMCQuotedata_BurnCost", conn_sales_new);
                    cmd.Parameters.Add("@PertiCular", SqlDbType.VarChar).Value = PertiCular;
                    cmd.Parameters.Add("@TotalAmount", SqlDbType.Decimal).Value = TotalAmount;
                    cmd.Parameters.Add("@NoOfClaim", SqlDbType.Decimal).Value = NoOfClaim;
                    cmd.Parameters.Add("@Acs", SqlDbType.Decimal).Value = Acs;
                    cmd.Parameters.Add("@versionNo", SqlDbType.Int).Value = versionNo;
                    cmd.CommandType = CommandType.StoredProcedure;
                    return await cmd.ExecuteNonQueryAsync();
                    

                }
                catch (Exception ex)
                {
                    throw;
                    await conn_sales_new.CloseAsync();
                    conn_sales_new.Dispose();
                }
                finally
                {
                    ds1.Dispose();
                    adapter.Dispose();
                   await conn_sales_new.CloseAsync();
                    conn_sales_new.Dispose();
                    //conn.Close();
                    //conn.Dispose();
                }


            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
               // conn.Close();
            }
        }
        public async Task<int> InsertFactorLoading(int PolicyLevelId, string Factors, string Loading, string Discount, string BurnAmt, string EnrollmentAmt, string ExpiringLimit, string ProposedLimit)
        {
            try
            {
                SqlConnection conn_sales_new = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds1 = new DataSet();
                SqlCommand cmd = new SqlCommand();
                try
                {

                    await conn_sales_new.OpenAsync();
                    cmd = new SqlCommand("udsp_QuoteVersion_LoadingDiscountFactor", conn_sales_new);
                    cmd.Parameters.Add("@PolicyLevelId", SqlDbType.VarChar).Value = PolicyLevelId;
                    cmd.Parameters.Add("@Factors", SqlDbType.VarChar).Value = Factors;
                    cmd.Parameters.Add("@Loading", SqlDbType.VarChar).Value = Loading;
                    cmd.Parameters.Add("@Discount", SqlDbType.VarChar).Value = Discount;
                    cmd.Parameters.Add("@BurnAmt", SqlDbType.VarChar).Value = BurnAmt;
                    cmd.Parameters.Add("@EnrollmentAmt", SqlDbType.VarChar).Value = EnrollmentAmt;
                    cmd.Parameters.Add("@ExpiringLimit", SqlDbType.VarChar).Value = ExpiringLimit;
                    cmd.Parameters.Add("@ProposedLimit", SqlDbType.VarChar).Value = ProposedLimit;
                    cmd.CommandType = CommandType.StoredProcedure;
                    return await cmd.ExecuteNonQueryAsync();


                }
                catch (Exception ex)
                {
                    throw;
                    await conn_sales_new.CloseAsync();
                    conn_sales_new.Dispose();
                }
                finally
                {
                    ds1.Dispose();
                    adapter.Dispose();
                    await conn_sales_new.CloseAsync();
                    conn_sales_new.Dispose();
                    //conn.Close();
                    //conn.Dispose();
                }


            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                // conn.Close();
            }
        }
        public async Task<int> InsertStanderdLoading(int PolicyLevelId, string LoadingFactor, string LoadingPer, string BurningLoading, string EnrollmentLoading)
        {
            try
            {
                SqlConnection conn_sales_new = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds1 = new DataSet();
                SqlCommand cmd = new SqlCommand();
                try
                {

                    await conn_sales_new.OpenAsync();
                    cmd = new SqlCommand("udsp_QuoteVersion_Standerd_Loading", conn_sales_new);
                    cmd.Parameters.Add("@PolicyLevelId", SqlDbType.VarChar).Value = PolicyLevelId;
                    cmd.Parameters.Add("@LoadingFactor", SqlDbType.VarChar).Value = LoadingFactor;
                    cmd.Parameters.Add("@LoadingPer", SqlDbType.VarChar).Value = LoadingPer;
                    cmd.Parameters.Add("@BurningLoading", SqlDbType.VarChar).Value = BurningLoading;
                    cmd.Parameters.Add("@EnrollmentLoading", SqlDbType.VarChar).Value = EnrollmentLoading;
                    cmd.CommandType = CommandType.StoredProcedure;
                    return await cmd.ExecuteNonQueryAsync();


                }
                catch (Exception ex)
                {
                    throw;
                    await conn_sales_new.CloseAsync();
                    conn_sales_new.Dispose();
                }
                finally
                {
                    ds1.Dispose();
                    adapter.Dispose();
                    await conn_sales_new.CloseAsync();
                    conn_sales_new.Dispose();
                    //conn.Close();
                    //conn.Dispose();
                }


            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                // conn.Close();
            }
        }
        public async Task<GMCCalculatorDetailsModel> InsertGMCRollover(GMCCalculatorDetailsModel model)
        {
            try
            {
                GMCCalculatorDetailsModel responce = new GMCCalculatorDetailsModel();
                SqlConnection conn_sales_new = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());
                SqlDataAdapter adapter = new SqlDataAdapter();
                DataSet ds1 = new DataSet();
                SqlCommand cmd = new SqlCommand();
                try
                {

                    await conn_sales_new.OpenAsync();
                    cmd = new SqlCommand("udsp_insertGMCQuotedata", conn_sales_new);
                    cmd.Parameters.Add("@PolicyNO", SqlDbType.VarChar).Value = model.PolicyNo;
                    cmd.Parameters.Add("@PolicyStartDate", SqlDbType.VarChar).Value = model.PolicyStartDate;
                    cmd.Parameters.Add("@PolicyEndDate", SqlDbType.VarChar).Value = model.Policy_End_date;
                    cmd.Parameters.Add("@ReconDate", SqlDbType.VarChar).Value = model.ReconDate;
                    cmd.Parameters.Add("@inception_premium", SqlDbType.Decimal).Value = model.IceptionPremium;
                    cmd.Parameters.Add("@FinalYearPremium", SqlDbType.Decimal).Value = model.FinalYearPremium;
                    cmd.Parameters.Add("@OpeningLives", SqlDbType.Int).Value = model.OpeningLives;
                    cmd.Parameters.Add("@ClosingLives", SqlDbType.Int).Value = model.ClosingLives;
                    cmd.Parameters.Add("@AvgLives", SqlDbType.Int).Value = model.AvgLives;
                    cmd.Parameters.Add("@OpeningEmp", SqlDbType.Int).Value = model.OpeningEmployee;
                    cmd.Parameters.Add("@ClosingEmp", SqlDbType.Int).Value = model.ClosingEmployee;
                    cmd.Parameters.Add("@AvgEmp", SqlDbType.Int).Value = model.AvgEmployee;
                    cmd.Parameters.Add("@InceptionPremiumPerlife", SqlDbType.Int).Value = model.InceptionPremiumperlife;
                    cmd.Parameters.Add("@ClosingPremiumPerlife", SqlDbType.Int).Value = model.ClosingPremiumperlife;

                    cmd.Parameters.Add("@PolicyServiceDay", SqlDbType.Int).Value = model.PolicyServiceDays;
                    cmd.Parameters.Add("@LSCSSubLimit", SqlDbType.Decimal).Value = model.LSCSSublimit==null?0: model.LSCSSublimit;
                    cmd.Parameters.Add("@LSCSSubLimit_result", SqlDbType.Decimal).Value = model.lscsLimitResult==null?0: model.lscsLimitResult;
                    cmd.Parameters.Add("@NormalSubLimit", SqlDbType.Decimal).Value = model.NormalSublimit==null?0: model.NormalSublimit;
                    cmd.Parameters.Add("@NormalSubLimit_result", SqlDbType.Decimal).Value = model.NormalLimitResult == null ? 0 : model.NormalLimitResult;
                    cmd.Parameters.Add("@Final_Enrollment_premium", SqlDbType.Decimal).Value = model.FinalEnrollmentpremium == null ? 0 : model.FinalEnrollmentpremium;
                    cmd.Parameters.Add("@CreatedBy", SqlDbType.VarChar).Value = userName;
                    cmd.Parameters.Add("@UW_remarks", SqlDbType.VarChar).Value = model.UWRemarks==null?"": model.UWRemarks;
                    cmd.Parameters.Add("@QuoteNumber", SqlDbType.VarChar).Value = model.QuoteNumber==null?"": model.QuoteNumber;
                    cmd.Parameters.Add("@VersionId", SqlDbType.Int);
                    cmd.Parameters["@VersionId"].Direction = ParameterDirection.Output;

                    cmd.CommandType = CommandType.StoredProcedure;
                    await cmd.ExecuteNonQueryAsync();
                    var versionNo = cmd.Parameters["@VersionId"].Value.ToString();
                    responce.version = versionNo;
                    return responce;

                }
                catch (Exception ex)
                {
                    throw;
                    await conn_sales_new.CloseAsync();
                    conn_sales_new.Dispose();
                }
                finally
                {
                    ds1.Dispose();
                    adapter.Dispose();
                    await conn_sales_new.CloseAsync();
                    conn_sales_new.Dispose();
                    //conn.Close();
                    //conn.Dispose();
                }


            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                // conn.Close();
            }
        }
        public async Task<DataSet> UpdateGridValue(string Factor, decimal ExistingLimit, decimal ProposedLimit, decimal BurnAmtPremium, decimal Enrollmentpremium)
        {
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();

            SqlConnection conn_sales = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());
            try
            {
                cmd.CommandText = "Udsp_GetFactor_LoadingAndDiscount_Rate";
                cmd.Parameters.Add("@factor", SqlDbType.VarChar, (500)).Value = Factor;
                cmd.Parameters.Add("@ExistingLimit", SqlDbType.Decimal).Value = ExistingLimit;
                cmd.Parameters.Add("@ProposedLimit", SqlDbType.Decimal).Value = ProposedLimit;
                cmd.Parameters.Add("@BurnAmtPremium", SqlDbType.Decimal).Value = BurnAmtPremium;
                cmd.Parameters.Add("@Enrollmentpremium", SqlDbType.Decimal).Value = Enrollmentpremium;


                cmd.Connection = conn_sales;//conn;
                await conn_sales.OpenAsync();
                cmd.CommandType = CommandType.StoredProcedure;
                adapter = new SqlDataAdapter(cmd);
                adapter.SelectCommand.CommandTimeout = 0;
                adapter.Fill(ds1);
                return ds1;
            }

            catch (Exception ex)
            {
                throw;
                await conn_sales.CloseAsync();
                conn_sales.Dispose();
            }
            finally
            {
                await conn_sales.CloseAsync();
                conn_sales.Dispose();
            }

        }
        public async Task<DataSet> BindVersionDetailsToControls(string Policyno, string VersionNumber)
        {


            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            SqlConnection conn_sales = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());

            try
            {
                cmd.CommandText = "udsp_GetGMC_QuoteVersionDetails_BindData";
                cmd.Parameters.Add("@policyno", SqlDbType.VarChar).Value = (Policyno);
                cmd.Parameters.Add("@VersionNumber", SqlDbType.VarChar).Value = (VersionNumber);
                cmd.Connection = conn_sales;//conn;
                await conn_sales.OpenAsync();
                cmd.CommandType = CommandType.StoredProcedure;
                adapter = new SqlDataAdapter(cmd);
                adapter.SelectCommand.CommandTimeout = 0;
                adapter.Fill(ds1);
                if (ds1.Tables[0].Rows.Count > 0)
                {



                    return ds1;

                }
                return ds1;
            }
            catch (Exception ex)
            {
                throw;
               await conn_sales.CloseAsync();
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
        public async Task<DataTable> BindMaternityCost(GMCCalculatorDetailsModel model)
        {
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            SqlConnection conn_sales = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());


            try
            {
                
                    cmd.CommandText = "udsp_Get_Maternity_Loading";
                    cmd.Parameters.Add("@LSCSLimit", SqlDbType.BigInt).Value = model.LSCSSublimit;
                    cmd.Parameters.Add("@NormalLimit", SqlDbType.BigInt).Value = model.NormalSublimit;
                    cmd.Connection = conn_sales;//conn;
                    conn_sales.Open();
                    cmd.CommandType = CommandType.StoredProcedure;
                    adapter = new SqlDataAdapter(cmd);
                    adapter.SelectCommand.CommandTimeout = 0;
                    adapter.Fill(ds1);
                    DataTable dt = new DataTable();
                    if (ds1.Tables[0].Rows.Count > 0)
                    {
                        
                        dt = ds1.Tables[0];

                      
                    }

                    return dt;
                
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
        public async Task<DataSet> BindBurnCost(GMCCalculatorDetailsModel model)
        {
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            SqlConnection conn_sales = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());


           
            try
            {

                string PoStartDate = model.PolicyStartDate.Date.ToString("dd/MM/yyyy");
                string PoEndDate = model.Policy_End_date.Date.ToString("dd/MM/yyyy");
                string ReconDate = model.ReconDate.Date.ToString("dd/MM/yyyy");

                cmd.CommandText = "udsp_Calculate_GMC_Data_Loading";
                cmd.Parameters.Add("@InceptionPremium", SqlDbType.VarChar).Value = (model.IceptionPremium);
                cmd.Parameters.Add("@FinalPremium", SqlDbType.VarChar).Value = (model.FinalYearPremium);
                cmd.Parameters.Add("@OpngLives", SqlDbType.VarChar).Value = (model.OpeningLives);
                cmd.Parameters.Add("@ClsgLives", SqlDbType.VarChar).Value = (model.ClosingLives);
                cmd.Parameters.Add("@Star_Rejection", SqlDbType.Decimal).Value = (model.startRejection);
                cmd.Parameters.Add("@IBNR_Userip", SqlDbType.VarChar).Value = (model.IBNR);
                cmd.Parameters.Add("@PolicyNo", SqlDbType.VarChar).Value = (model.PolicyNo);
                cmd.Parameters.Add("@PoStartDate", SqlDbType.VarChar).Value = (PoStartDate);
                cmd.Parameters.Add("@PoEndDate", SqlDbType.VarChar).Value = (PoEndDate);
                cmd.Parameters.Add("@ReconDate", SqlDbType.VarChar).Value = (ReconDate);
                cmd.Parameters.Add("@Opening_Employee", SqlDbType.VarChar).Value = (model.OpeningEmployee);
                cmd.Parameters.Add("@Closing_Employee", SqlDbType.VarChar).Value = (model.ClosingEmployee);
                cmd.Parameters.Add("@Avg_Employee", SqlDbType.VarChar).Value = (model.AvgEmployee);
                cmd.Connection = conn_sales;//conn;
                await conn_sales.OpenAsync();
                cmd.CommandType = CommandType.StoredProcedure;
                adapter = new SqlDataAdapter(cmd);
                adapter.SelectCommand.CommandTimeout = 0;
                adapter.Fill(ds1);
                DataTable dt = new DataTable();
                if (ds1.Tables[0].Rows.Count > 0)
                {
                    
                    //DataTable dtloading = new DataTable();
                    //DataTable dtloadingFactor = new DataTable();
                    //dt = ds1.Tables[0];
                    return ds1;


                }
                return ds1;
            }
            catch (Exception ex)
            {
               
               await conn_sales.CloseAsync();
                conn_sales.Dispose();
                 throw;
            }
            finally
            {
                ds1.Dispose();
                adapter.Dispose();
               await conn_sales.CloseAsync();
                conn_sales.Dispose();
            }
        }
        public async Task<DataSet> BindBurnCostForRenewal(GMCCalculatorDetailsModel model)
        {
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            SqlConnection conn_sales = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());



            try
            {

               
                cmd.CommandText = "[udsp_Calculate_GMC_Data_Loading_renewal]";

                cmd.Parameters.Add("@PolicyNo", SqlDbType.VarChar).Value = (model.PolicyNo);
                cmd.Connection = conn_sales;//conn;
                await conn_sales.OpenAsync();
                cmd.CommandType = CommandType.StoredProcedure;
                adapter = new SqlDataAdapter(cmd);
                adapter.SelectCommand.CommandTimeout = 0;
                adapter.Fill(ds1);
                DataTable dt = new DataTable();
                if (ds1.Tables[0].Rows.Count > 0)
                {

                    //DataTable dtloading = new DataTable();
                    //DataTable dtloadingFactor = new DataTable();
                    //dt = ds1.Tables[0];
                    return ds1;


                }
                return ds1;
            }
            catch (Exception ex)
            {

                await conn_sales.CloseAsync();
                conn_sales.Dispose();
                throw;
            }
            finally
            {
                ds1.Dispose();
                adapter.Dispose();
                await conn_sales.CloseAsync();
                conn_sales.Dispose();
            }
        }
        public async Task<DataTable> BindVersionDetails(string PolicyNo)
        {
            // string PolicyNo;
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            SqlConnection conn_sales = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());
            //if (txtPolicyNo.Text == "")
            //{
            //    lblMessage.Text = "Please select PolicyNo";
            //    return;
            //}
            //PolicyNo = txtPolicyNo.Text;
            try
            {
                cmd.CommandText = "udsp_GetGMC_QuoteVersionDetails";
                cmd.Parameters.Add("@policyno", SqlDbType.VarChar).Value = (PolicyNo);
                cmd.Connection = conn_sales;//conn;
                await conn_sales.OpenAsync();
                cmd.CommandType = CommandType.StoredProcedure;
                adapter = new SqlDataAdapter(cmd);
                adapter.SelectCommand.CommandTimeout = 0;
                adapter.Fill(ds1);
                DataTable dt = new DataTable();
                if (ds1.Tables[0].Rows.Count > 0)
                {

                    
                    dt = ds1.Tables[0];
                    


                }
                return dt;

            }
            catch (Exception ex)
            {
               
                await conn_sales.CloseAsync();


                conn_sales.Dispose();
                throw;
            }
            finally
            {
                ds1.Dispose();
                adapter.Dispose();
                await conn_sales.CloseAsync();
                conn_sales.Dispose();
            }
        }
        public async Task<GMCCalculatorDetailsModel> DownloadRenewalVersionDetails(string Policyno, string VersionNumber)
        {
            GMCCalculatorDetailsModel responce = new GMCCalculatorDetailsModel();
            // string PolicyNo;
            SqlDataAdapter adapter = new SqlDataAdapter();
            SqlDataAdapter adapter1 = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            DataSet ds2 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            SqlCommand cmd1 = new SqlCommand();
            SqlConnection conn_sales = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());
            SqlConnection ConnectionToGPS = new SqlConnection(_configuration["ConnectionStrings:ConnectionToGPS"].ToString());
            try
            {
                cmd.CommandText = "udsp_GetGMC_QuoteVersionDetails_BindData";
                cmd.Parameters.Add("@policyno", SqlDbType.VarChar).Value = (Policyno);
                cmd.Parameters.Add("@VersionNumber", SqlDbType.VarChar).Value = (VersionNumber);
                cmd.Connection = conn_sales;//conn;
                await conn_sales.OpenAsync();
                cmd.CommandType = CommandType.StoredProcedure;
                adapter = new SqlDataAdapter(cmd);
                adapter.SelectCommand.CommandTimeout = 0;
                adapter.Fill(ds1);
                DataTable dt = new DataTable();
                if (ds1.Tables[0].Rows.Count > 0)
                {

                    responce.firstDataset = ds1;    
                    


                }
                cmd1.CommandText = "SP_get_GMCTearmsAndCondition";
                cmd1.Parameters.Add("@policyno", SqlDbType.VarChar).Value = (Policyno);
                cmd1.Connection = ConnectionToGPS;//conn;
                ConnectionToGPS.Open();
                cmd1.CommandType = CommandType.StoredProcedure;
                adapter1 = new SqlDataAdapter(cmd1);
                adapter1.SelectCommand.CommandTimeout = 0;
                adapter1.Fill(ds2);
                if (ds2.Tables[0].Rows.Count > 0)
                {


                    responce.seconfDataset = ds2;


                }
                return responce;

            }
            catch (Exception ex)
            {

                await conn_sales.CloseAsync();


                conn_sales.Dispose();
                throw;
            }
            finally
            {
                ds1.Dispose();
                adapter.Dispose();
                await conn_sales.CloseAsync();
                conn_sales.Dispose();
            }
        }
        public async Task<DataSet> DownloadVersionDetails(string Policyno, string VersionNumber)
        {
            GMCCalculatorDetailsModel responce = new GMCCalculatorDetailsModel();
            // string PolicyNo;
            SqlDataAdapter adapter = new SqlDataAdapter();
            
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            SqlConnection conn_sales = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());
           
            try
            {
                cmd.CommandText = "udsp_GetGMC_QuoteVersionDetails_BindData";
                cmd.Parameters.Add("@policyno", SqlDbType.VarChar).Value = (Policyno);
                cmd.Parameters.Add("@VersionNumber", SqlDbType.VarChar).Value = (VersionNumber);
                cmd.Connection = conn_sales;//conn;
                await conn_sales.OpenAsync();
                cmd.CommandType = CommandType.StoredProcedure;
                adapter = new SqlDataAdapter(cmd);
                adapter.SelectCommand.CommandTimeout = 0;
                adapter.Fill(ds1);
                
                
                return ds1;

            }
            catch (Exception ex)
            {

                await conn_sales.CloseAsync();


                conn_sales.Dispose();
                throw;
            }
            finally
            {
                ds1.Dispose();
                adapter.Dispose();
                await conn_sales.CloseAsync();
                conn_sales.Dispose();
            }
        }
        public async Task<DataSet> DownloadRenewalSummeryVersionDetails(string Policyno, string VersionNumber)
        {
            GMCCalculatorDetailsModel responce = new GMCCalculatorDetailsModel();
            // string PolicyNo;
            SqlDataAdapter adapter = new SqlDataAdapter();

            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            SqlConnection conn_sales = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());

            try
            {
                cmd.CommandText = "udsp_GetGMC_DownloadSummary";
                cmd.Parameters.Add("@policyno", SqlDbType.VarChar).Value = (Policyno);
                cmd.Parameters.Add("@VersionNumber", SqlDbType.VarChar).Value =
                    string.IsNullOrWhiteSpace(VersionNumber) ? DBNull.Value : VersionNumber;
                cmd.Connection = conn_sales;//conn;
                await conn_sales.OpenAsync();
                cmd.CommandType = CommandType.StoredProcedure;
                adapter = new SqlDataAdapter(cmd);
                adapter.SelectCommand.CommandTimeout = 0;
                adapter.Fill(ds1);
                DataTable dt = new DataTable();
                if (ds1.Tables[0].Rows.Count > 0)
                {

                    dt = ds1.Tables[0];



                }

                return ds1;

            }
            catch (Exception ex)
            {

                await conn_sales.CloseAsync();


                conn_sales.Dispose();
                throw;
            }
            finally
            {
                ds1.Dispose();
                adapter.Dispose();
                await conn_sales.CloseAsync();
                conn_sales.Dispose();
            }
        }
        public async Task<DataTable> GetGMCPolicyLevelData(string PolicyNo)
        {
            SqlConnection conn_sales = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            try
            {
                cmd.CommandText = "udsp_Get_GMC_PolicyLevelData";
                cmd.Parameters.Add("@PolicyNO", SqlDbType.VarChar).Value = PolicyNo;
                cmd.Connection = conn_sales;//conn;
               await conn_sales.OpenAsync();
                cmd.CommandType = CommandType.StoredProcedure;
                adapter = new SqlDataAdapter(cmd);
                adapter.SelectCommand.CommandTimeout = 0;
                adapter.Fill(ds1);
                DataTable dt = new DataTable();
                if (ds1.Tables[0].Rows.Count > 0)
                {
                   
                    dt = ds1.Tables[0];
                   
                }
                return dt;
            }
            catch 
            {
                DataTable dt = new DataTable();
                await conn_sales.CloseAsync();
                conn_sales.Dispose();
                return dt;
            }
            finally
            {
                ds1.Dispose();
                adapter.Dispose();
               await conn_sales.CloseAsync();
                conn_sales.Dispose();
            }
        }
        public async Task<DataSet> GetTrendAnalysis(string PolicyNo, string FYYear)
        {
            SqlConnection conn_sales = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            try
            {
                cmd.CommandText = "udsp_GetGMC_TrendAnalysis";
                cmd.Parameters.Add("@PolicyNo", SqlDbType.VarChar).Value =
                    string.IsNullOrWhiteSpace(PolicyNo) ? DBNull.Value : PolicyNo;
                cmd.Parameters.Add("@FYYear", SqlDbType.VarChar).Value =
                    string.IsNullOrWhiteSpace(FYYear) ? DBNull.Value : FYYear;
                cmd.Connection = conn_sales;
                await conn_sales.OpenAsync();
                cmd.CommandType = CommandType.StoredProcedure;
                adapter = new SqlDataAdapter(cmd);
                adapter.SelectCommand.CommandTimeout = 0;
                adapter.Fill(ds1);
                return ds1;
            }
            finally
            {
                adapter.Dispose();
                await conn_sales.CloseAsync();
                conn_sales.Dispose();
            }
        }
        public async Task<DataTable> GetGMCRolloverLiveData(string PolicyNo)
        {
            SqlConnection conn_sales = new SqlConnection(_configuration["ConnectionStrings:ConnectionToTele_Dashboard"].ToString());
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds1 = new DataSet();
            SqlCommand cmd = new SqlCommand();
            try
            {
                cmd.CommandText = "sp_get_rollover_lives";
                cmd.Parameters.Add("@PolicyNO", SqlDbType.VarChar).Value = PolicyNo;
                cmd.Connection = conn_sales;//conn;
                await conn_sales.OpenAsync();
                cmd.CommandType = CommandType.StoredProcedure;
                adapter = new SqlDataAdapter(cmd);
                adapter.SelectCommand.CommandTimeout = 0;
                adapter.Fill(ds1);
                DataTable dt = new DataTable();
                if (ds1.Tables[0].Rows.Count > 0)
                {

                    dt = ds1.Tables[0];

                }
                return dt;
            }
            catch
            {
                DataTable dt = new DataTable();
                await conn_sales.CloseAsync();
                conn_sales.Dispose();
                return dt;
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
