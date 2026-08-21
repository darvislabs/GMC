using System.Data;
using System.Globalization;
using GMC.Helper;
using GMC.Interface;
using GMC.Interface.GMC;
using GMC.Models.GMC;

namespace GMC.DAL.Repository.GMC
{
    public class GMCUploaderRepo : IGMCUploaderRepo
    {
        readonly IConfiguration _config;
        readonly ISqlHelperQuery _sql;
        readonly CommonBAL _common;
        readonly GMCUploaderDAL _DAL;
        string con = "";
        readonly IWebHostEnvironment _hostingEnv;
        private readonly IHttpContextAccessor _httpContextAccessor;
        string UserName = "";
        public GMCUploaderRepo(IHttpContextAccessor httpContextAccessor, IConfiguration config, ISqlHelperQuery sql, CommonBAL common, GMCUploaderDAL DAL, IWebHostEnvironment hostingEnv)
        {
            _config = config;
            _sql = sql;
            _common = common;
            con = _config["ConnectionStrings:ConnectionToTele_Dashboard"];
            _DAL = DAL;
            _hostingEnv = hostingEnv;
            _httpContextAccessor=httpContextAccessor;
            //UserName = _httpContextAccessor.HttpContext.User.FindFirst("UserName").Value;

        }
        public GMCUploaderModel getIndustryName()
        {
            try
            {
                GMCUploaderModel responce = new GMCUploaderModel();
                DataTable dt = _sql.GetDataTable(con, "select [Nature of Industry] as Nature_of_Industry from tbl_GMC_industry_master(nolock) order by [Nature of Industry]");
                responce.industryNameList = DataTableToList.ConvertDataTableToListForCommon<industryName>(dt);
                return responce;
            }
            catch (Exception ee)
            {

                throw;
            }
            
        }

        public List<string> getSearchInsuraceCompanyName(string prifix)
        {
            // Defence-in-depth: the underlying SqlHelperQuery does not expose a
            // parameterised reader, so we sanitise the autocomplete prefix to a
            // conservative alphanumeric subset before composing the LIKE clause.
            var safe = SanitisePrefix(prifix);
            if (string.IsNullOrEmpty(safe)) return new List<string>();
            List<string> result = _sql.GetExecuteReader(con,
                "select company_name from tbl_company_list with(nolock) where company_name like '" + safe + "%'",
                "company_name");
            return result;
        }

        public List<string> getSearchTPA(string prifix)
        {
            var safe = SanitisePrefix(prifix);
            if (string.IsNullOrEmpty(safe)) return new List<string>();
            List<string> result = _sql.GetExecuteReader(con,
                "select TPA_Name from tbl_TPA_list with(nolock) where TPA_Name like '" + safe + "%'",
                "TPA_Name");
            return result;
        }

        /// <summary>
        /// Keeps only letters, digits, spaces, hyphens and ampersands — the
        /// characters reasonable to find inside company / TPA names.  Anything
        /// else (single-quote, semicolon, brackets, control chars …) is dropped.
        /// </summary>
        private static string SanitisePrefix(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            raw = raw.Trim();
            if (raw.Length > 64) raw = raw[..64];
            var buf = new char[raw.Length];
            int n = 0;
            foreach (var c in raw)
            {
                if (char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '&' || c == '.')
                    buf[n++] = c;
            }
            return new string(buf, 0, n);
        }

        public async Task<GMCUploaderModel> uploadData(GMCUploaderModel model)
        {
            GMCUploaderModel responce = new GMCUploaderModel();
            try
            {
            string startdatetime = DateTime.Now.ToString();
            DateTime startDateTime = DateTime.Now;
            
            string DownloadDt = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt", CultureInfo.CreateSpecificCulture("en-US"));
            DownloadDt = DownloadDt.Replace(" ", "_");
            DownloadDt = DownloadDt.Replace(":", "_");
            if (model.bussinessType == "2" && model.unit == "0")
            {
                int result =await _DAL.insert_renewal_enrollmentdata(model);
                if (result>0)
                {
                    responce.message = "data save successfully............";
                    return responce;
                }
               
            }
            else if(model.bussinessType=="0"|| model.bussinessType == "1"|| (model.bussinessType == "2" && model.unit == "1"))
            {
                string uploadpath = Path.Combine(this._hostingEnv.WebRootPath, @"upload/GMCUploader");
                string dest_path = uploadpath;
                if (!Directory.Exists(dest_path))
                {
                    Directory.CreateDirectory(dest_path);
                }
                string sourcefile = Path.GetFileName("Upload_at_" + DownloadDt + "_" + model.myFile.FileName);
                string path = Path.Combine(dest_path, sourcefile);
                using (FileStream filestream = new FileStream(path, FileMode.Create))
                {
                    model.myFile.CopyTo(filestream);
                }

                DataTable DT = await _common.GetDataFromExcel(path);
                
                if (model.typeofData == "Claim")
                {
                    _DAL.insert_log("Claim", model.strPolicyNo);
                    _DAL.truncateTable("Claim", model.strPolicyNo);

                }
                else
                {
                    _DAL.insert_log("endroll", model.strPolicyNo);
                    _DAL.truncateTable("endroll", model.strPolicyNo);

                }
                string[] columnNames = DT.Columns.Cast<DataColumn>()
                                 .Select(x => x.ColumnName)
                                 .ToArray();
                var typeofdata = "";
                if (model.typeofData == "Claim")
                {
                    typeofdata = model.typeofData;
                }
                string FileParameter = string.Join(",", Array.ConvertAll<object, string>(columnNames.ToArray(), Convert.ToString));

               string Parameter = FileParameter;
                DataSet MYXLS =await _DAL.GetGMCParameter(FileParameter, typeofdata);
                DataTable dtmatchRow = new DataTable();
                dtmatchRow = MYXLS.Tables[0];
                var MasterColumn = dtmatchRow.AsEnumerable().Select(r => r["MasterColumn"].ToString());
                string MasterColumnvalue = string.Join(",", MasterColumn);
                var inputColumn = dtmatchRow.AsEnumerable().Select(r => r["ipcolumn"].ToString());
                string inputColumnvalue = string.Join(",", inputColumn);
                List<string> ExcelColumn = inputColumnvalue.Split(',').ToList<string>();
                string[] select = ExcelColumn.ToArray();
                select = select.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                DataTable dt3 = new DataView(DT).ToTable(false, select);
                DataTable Masterparameter = new DataTable();
                Masterparameter = MYXLS.Tables[0];
                DataTable dt1 = new DataTable();
                dt1 = removeNullColumnFromDataTable_data(Masterparameter);
                Int32 uploadedColumnFlag = 0;
                Int32 checkflag = 0;
                foreach (DataRow row in dt1.Rows)
                {
                    uploadedColumnFlag = row.Field<Int32>(2);
                    if (uploadedColumnFlag == 0)
                    {
                        checkflag = 1;
                    }
                }

                var inputColumn_new = dt1.AsEnumerable().Select(r => r["ipcolumn"].ToString());
                string inputColumnvalue_new = string.Join(",", inputColumn);
                var string1 = inputColumnvalue;
                var string2 = inputColumnvalue_new;
                var Result = string.Join(",", string1.Split(',').Except(string2.Split(',')));

                List<string> DeleteColumn = Result.Split(',').ToList<string>();
                string[] Deleteselect = DeleteColumn.ToArray();


                //string[] ColumnsToBeDeleted = { Result };

                foreach (string ColName in Deleteselect)
                {
                    if (DT.Columns.Contains(ColName))
                        DT.Columns.Remove(ColName);
                }
                if (checkflag == 1)
                {
                    List<columnName> cc = new List<columnName>();
                    foreach (var item in columnNames)
                    {
                        cc.Add(new columnName { column = item.Trim() });
                    }
                    responce.columnList = cc;
                    responce.errorDT=dt1;
                    return responce;

                }
                else
                {
                    foreach (DataColumn column in DT.Columns)
                    {
                        string colname = column.ColumnName;

                        foreach (DataRow row in dt1.Rows)
                        {
                            var field1 = row.Field<string>(1);
                            if (field1 == colname)
                            {

                                var field2 = row.Field<string>(0);
                                dt3.Columns[colname].ColumnName = field2;

                            }


                        }

                    }
                    foreach (string ColName in Deleteselect)
                    {
                        if (dt3.Columns.Contains(ColName))
                            dt3.Columns.Remove(ColName);
                    }
                    if (model.typeofData == "Claim")
                    {
                        var x = (from r in dt3.AsEnumerable()
                                 select r["ClaimStatus"]).Distinct().ToList();

                        string namelist = String.Join("*", x.ToArray());
                        DataSet ds_Claim = new DataSet();
                        ds_Claim = _DAL.GetGMCPendingClaimStatus(namelist);
                        if (ds_Claim.Tables[0].Rows.Count > 0)
                        {
                                DataView dv = new DataView(ds_Claim.Tables[0]);
                                dv.RowFilter = "ipcolumn=''";
                                if (dv.Table.Rows.Count>0)
                                {
                                    string cMaster = "";
                                    foreach (DataRow row in ds_Claim.Tables[0].Rows)
                                    {
                                        cMaster += row["MasterColumn"].ToString()+",";
                                    }
                                    cMaster = cMaster.TrimEnd(',');
                                    responce.message = cMaster+" invalid Columns, Please check";
                                    return responce;

                                }
                            responce.errorDT = ds_Claim.Tables[0];
                            responce.tablename = "Claim";
                            return responce;


                        }

                    }
                       string PolicyNo = model.strPolicyNo;

                        dt3.Columns.Add("PolicyNo_unique", typeof(System.String));
                        foreach (DataRow row in dt3.Rows)
                        {
                            //need to set value to NewColumn column
                            row["PolicyNo_unique"] = PolicyNo;   // or set it to some other value
                        }
                       string  PolicyName = model.strPolicyName;

                        dt3.Columns.Add("PolicyName", typeof(System.String));
                        foreach (DataRow row in dt3.Rows)
                        {
                            //need to set value to NewColumn column
                            row["PolicyName"] = PolicyName;   // or set it to some other value
                        }

                       string CompanyName = model.InsuranceCompanyName;

                        dt3.Columns.Add("Insurance_Company_Name", typeof(System.String));
                        foreach (DataRow row in dt3.Rows)
                        {
                            //need to set value to NewColumn column
                            row["Insurance_Company_Name"] = CompanyName;   // or set it to some other value
                        }
                       string TPA = model.TPA;

                        dt3.Columns.Add("TPA", typeof(System.String));
                        foreach (DataRow row in dt3.Rows)
                        {
                            //need to set value to NewColumn column
                            row["TPA"] = TPA;   // or set it to some other value
                        }
                       string  SubType = Enum.GetName(typeof(subType), int.Parse(model.subType));
                    dt3.Columns.Add("SubType", typeof(System.String));

                        foreach (DataRow row in dt3.Rows)
                        {
                        //need to set value to NewColumn column
                        row["SubType"] = SubType;   // or set it to some other value
                        }
                        string Industry_Name = model.industryName;
                        dt3.Columns.Add("Industry_Name", typeof(System.String));

                        foreach (DataRow row in dt3.Rows)
                        {
                            //need to set value to NewColumn column
                            row["Industry_Name"] = Industry_Name;   // or set it to some other value
                        }
                       string Bussiness_type = Enum.GetName(typeof(bussinessCType), int.Parse(model.bussinessType));
                    dt3.Columns.Add("Bussiness_type", typeof(System.String));

                        foreach (DataRow row in dt3.Rows)
                        {
                        //need to set value to NewColumn column
                        row["Bussiness_type"] = Bussiness_type;   // or set it to some other value
                        }

                        dt3.Columns.Add("created_by", typeof(System.String));

                        foreach (DataRow row in dt3.Rows)
                        {
                            //need to set value to NewColumn column
                            row["created_by"] = UserName;   // or set it to some other value
                        }

                        dt3.Columns.Add("insert_date", typeof(System.String));

                        foreach (DataRow row in dt3.Rows)
                        {
                            //need to set value to NewColumn column
                            row["insert_date"] = startdatetime;   // or set it to some other value
                        }
                        string columns = string.Join(","
          , dt3.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
                        string values = string.Join(","
                            , dt3.Columns.Cast<DataColumn>().Select(c => string.Format("@{0}", c.ColumnName)));
                        string result = _DAL.insertdata(model,columns,values,dt3);
                       
                       responce.message = result;
                        return responce;
                       
                    
                }
                int versioncount1 = _DAL.checkVersionList(model.strPolicyNo);
                if (versioncount1 > 0)
                {
                    responce.message = "Quote is already created for this policy, Please check pending page.";
                    return responce;

                }
            }
            _DAL.Update_SumInsured(model.strPolicyNo);
            return responce;

            }
            catch (Exception ee)
            {

                responce.message = ee.Message.ToString(); 
                return responce;
            }

        }
        public static DataTable removeNullColumnFromDataTable_data(DataTable dt)
        {
            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                if (dt.Rows[i][0].ToString() == "")
                    dt.Rows[i].Delete();
                dt.AcceptChanges();
            }
            return dt;
        }

        public async Task<GMCUploaderModel> updateMaster(List<updatecolumnName> model, string typeofData,string tablename)
        {
            GMCUploaderModel responce=new GMCUploaderModel();
            var m = model.Select(x => x.masterColumn).ToList();
            var u = model.Select(x => x.updateColumn).ToList();
            string mastercolumn = string.Join(",", m);
            string updatecolumn = string.Join(",", u);
            string MasterParameter = mastercolumn + "*" + updatecolumn;
            int result = await _DAL.UpdateMaster(typeofData, MasterParameter,tablename);
            if(result>0)
            {
                responce.message = "Records inserted successfully";
                return responce;
            }
            responce.message = "Records not inserted successfully";
            return responce;


        }
    }
}
