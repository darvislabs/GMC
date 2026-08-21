using System.Data;
using System.Globalization;
using GMC.Helper;
using GMC.Interface.GMC;
using GMC.Models.GMC;

namespace GMC.DAL.Repository.GMC
{
    public class GMCCalculatorDetailsRepo : IGMCCalculatorDetailsRepo
    {
        readonly GMCCalculatorDetailsDAL _DAL;
        readonly IWebHostEnvironment _hosting;
        readonly CommonBAL _cBAL;
        public GMCCalculatorDetailsRepo(GMCCalculatorDetailsDAL dAL, IWebHostEnvironment hosting, CommonBAL cBAL)
        {
            _DAL = dAL;
            _hosting = hosting;
            _cBAL = cBAL;
        }
        public async Task<DataSet> BindBurnCost(GMCCalculatorDetailsModel model)
        {
            return await _DAL.BindBurnCost(model);
        }

        public async Task<DataSet> BindBurnCostForRenewal(GMCCalculatorDetailsModel model)
        {
            return await _DAL.BindBurnCostForRenewal(model);
        }

        public async Task<DataTable> BindMaternityCost(GMCCalculatorDetailsModel model)
        {
            return await _DAL.BindMaternityCost(model);
        }

        public async Task<DataTable> BindVersionDetails(string PolicyNo)
        {
            return await _DAL.BindVersionDetails(PolicyNo);
        }

        public async Task<DataSet> BindVersionDetailsToControls(string Policyno, string VersionNumber)
        {
            return await _DAL.BindVersionDetailsToControls(Policyno, VersionNumber);
        }

        public async Task<GMCCalculatorDetailsModel> DownloadRenewalVersionDetailsToControls(string Policyno, string VersionNumber)
        {
            GMCCalculatorDetailsModel model = new GMCCalculatorDetailsModel();

            string DownloadDt = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt", CultureInfo.CreateSpecificCulture("en-US"));
            DownloadDt = DownloadDt.Replace(" ", "_");
            DownloadDt = DownloadDt.Replace(":", "_");
            string FileNameRedirect = "GMC_Quotes" + "_at_" + DownloadDt;
            FileNameRedirect = "RenewalPolicy" + "_" + VersionNumber + ".xlsx";
            string uploadpath = Path.Combine(this._hosting.WebRootPath, @"ReportDownload\");
            string FileName = uploadpath + FileNameRedirect;
            GMCCalculatorDetailsModel ds = await _DAL.DownloadRenewalVersionDetails(Policyno, VersionNumber);
            if (ds.firstDataset.Tables[0].Rows.Count > 0)
            {
                _cBAL.EPPlusExportDS(ds.firstDataset, ds.seconfDataset, FileName);
                model.excelfileName = FileName;
                return model;
            }
            model.error = "Data not found";
            return model;
        }

        public async Task<GMCCalculatorDetailsModel> DownloadSummeryVersionDetailsToControls(string Policyno, string VersionNumber)
        {
            try
            {
                GMCCalculatorDetailsModel model = new GMCCalculatorDetailsModel();

                string DownloadDt = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt", CultureInfo.CreateSpecificCulture("en-US"));
                DownloadDt = DownloadDt.Replace(" ", "_");
                DownloadDt = DownloadDt.Replace(":", "_");
                string FileNameRedirect = "GMC_Summary" + "_" +
                    string.Join("_", Policyno.Split(Path.GetInvalidFileNameChars(),
                        StringSplitOptions.RemoveEmptyEntries)) + "_" + VersionNumber + "_at_" + DownloadDt + ".xlsx";
                string uploadpath = Path.Combine(this._hosting.WebRootPath, @"ReportDownload\");
                Directory.CreateDirectory(uploadpath);
                string FileName = uploadpath + FileNameRedirect;
                DataSet ds = await _DAL.DownloadRenewalSummeryVersionDetails(Policyno, VersionNumber);
                if (ds.Tables.Count >= 8 && ds.Tables[0].Rows.Count > 0)
                {
                    _cBAL.EPPlusExportGmcSummary(ds, FileName);
                    model.excelfileName = FileName;
                    return model;
                }
                model.error = "Data not found";
                return model;

            }
            catch (Exception)
            {

                throw;
            }

        }

        public async Task<GMCCalculatorDetailsModel> DownloadVersionDetailsToControls(string Policyno, string VersionNumber)
        {
            GMCCalculatorDetailsModel model = new GMCCalculatorDetailsModel();

            string DownloadDt = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt", CultureInfo.CreateSpecificCulture("en-US"));
            DownloadDt = DownloadDt.Replace(" ", "_");
            DownloadDt = DownloadDt.Replace(":", "_");
            string FileNameRedirect = "GMC_Quotes" + "_at_" + DownloadDt;
            FileNameRedirect = "GMCPolicy" + "_" + VersionNumber + ".xlsx";
            string uploadpath = Path.Combine(this._hosting.WebRootPath, @"ReportDownload\");
            string FileName = uploadpath + FileNameRedirect;
            DataSet ds = await _DAL.DownloadVersionDetails(Policyno, VersionNumber);
            if (ds.Tables[1].Rows.Count > 0)
            {
                _cBAL.EPPlusExportDS(ds, FileName);
                model.excelfileName = FileName;
                return model;
            }
            model.error = "Data not found";
            return model;
        }

        public async Task<DataTable> GetGMCPolicyLevelData(string PolicyNo)
        {
            return await _DAL.GetGMCPolicyLevelData(PolicyNo);
        }

        public async Task<DataSet> GetTrendAnalysis(string PolicyNo, string FYYear)
        {
            return await _DAL.GetTrendAnalysis(PolicyNo, FYYear);
        }
        public async Task<DataTable> GetGMCRolloverLiveData(string PolicyNo)
        {
            return await _DAL.GetGMCRolloverLiveData(PolicyNo);
        }

        public async Task<GMCCalculatorDetailsModel> InsertGMCRollover(GMCCalculatorDetailsModel model)
        {
            return await _DAL.InsertGMCRollover(model);
        }

        public async Task<DataTable> PolicyPendingDetails(string PolicyNo)
        {
            return await _DAL.policyPendingDetails(PolicyNo);
        }

        public async Task<int> SaveBurnCostDetails(List<string[]> Sdata, string version)
        {
            foreach (var item in Sdata)
            {
                await _DAL.InsertBurnCostData(item[0].ToString(), Convert.ToDecimal(item[1].ToString()), Convert.ToDecimal(item[2].ToString()), Convert.ToDecimal(item[3].ToString()), version);
            }
            return 1;
        }

        public async Task<int> SaveLoadFactorDetails(List<string[]> Sdata, string version)
        {
            foreach (var item in Sdata)
            {
                await _DAL.InsertFactorLoading(Convert.ToInt16(version), item[0].ToString(), item[1] == null ? "" : item[1].ToString(),
                    item[2] == null ? "" : item[2].ToString(), item[3].ToString(), item[4].ToString(), item[5] == null ? "" : item[5].ToString(), item[6] == null ? "" : item[6].ToString());
            }
            return 1;
        }

        public async Task<int> SaveLoadStanderdDetails(List<string[]> Sdata, string version)
        {
            foreach (var item in Sdata)
            {
                await _DAL.InsertStanderdLoading(Convert.ToInt16(version), item[0].ToString(), item[1].ToString(), item[2].ToString(), item[3].ToString());
            }
            return 1;
        }

        public async Task<DataSet> UpdateGridValue(string Factor, decimal ExistingLimit, decimal ProposedLimit, decimal BurnAmtPremium, decimal Enrollmentpremium)
        {
            return await _DAL.UpdateGridValue(Factor, ExistingLimit, ProposedLimit, BurnAmtPremium, Enrollmentpremium);
        }

        public async Task<DataSet> UpdateGridValue_new(string Factor, DataTable DT, decimal BurnAmtPremium, decimal Enrollmentpremium, string policyno)
        {
            return await _DAL.UpdateGridValue_new(Factor, DT, BurnAmtPremium, Enrollmentpremium, policyno);
        }

        public async Task<DataSet> UpdateGridValue_new_lives(string Factor, DataTable DT, decimal BurnAmtPremium, decimal Enrollmentpremium, string policyno)
        {
            return await _DAL.UpdateGridValue_new_lives(Factor, DT, BurnAmtPremium, Enrollmentpremium, policyno);
        }

        public async Task<GMCCalculatorDetailsModel> uploadFile(IFormFile myfilename, IFormFile filename1, string policyno)
        {
            GMCCalculatorDetailsModel model = new GMCCalculatorDetailsModel();
            if (myfilename != null)
            {
                string uploadpath = Path.Combine(this._hosting.WebRootPath, @"GMCUpload");
                string dest_path = uploadpath;
                if (!Directory.Exists(dest_path))
                {
                    Directory.CreateDirectory(dest_path);
                }
                string sourcefile = Path.GetFileName(myfilename.FileName);
                string path = Path.Combine(dest_path, sourcefile);
                using (FileStream filestream = new FileStream(path, FileMode.Create))
                {
                    myfilename.CopyTo(filestream);
                }
                DataTable dt = await _cBAL.GetDataFromExcel(path);
                if (dt.Rows.Count > 0)
                {
                    await _DAL.uploadXlsFile(dt, policyno);
                }
                model.rolloverSIDT = dt;
                //model.FileName = model.myFile.FileName;
                //model.FilePath = path;
            }
            if (filename1 != null)
            {
                string uploadpath = Path.Combine(this._hosting.WebRootPath, @"GMCUpload");
                string dest_path = uploadpath;
                if (!Directory.Exists(dest_path))
                {
                    Directory.CreateDirectory(dest_path);
                }
                string sourcefile = Path.GetFileName(filename1.FileName);
                string path = Path.Combine(dest_path, sourcefile);
                using (FileStream filestream = new FileStream(path, FileMode.Create))
                {
                    filename1.CopyTo(filestream);
                }
                DataTable dt = await _cBAL.GetDataFromExcel(path);
                if (dt.Rows.Count > 0)
                {
                    await _DAL.uploadXlsFile2(dt, policyno);
                }
                model.rolloverDT = dt;
                //model.FileName = model.myFile.FileName;
                //model.FilePath = path;
            }
            return model;
        }
    }
}
