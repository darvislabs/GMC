using System.Globalization;
using GMC.Helper;
using GMC.Interface;
using GMC.Interface.GMC;
using GMC.Models.GMC;
using OfficeOpenXml;

namespace GMC.BL.GMC
{
    /// <summary>
    /// Business rules for the Sales-Person upload step:
    ///     * accept only .xlsx
    ///     * persist file under wwwroot/upload/GMCRollover/
    ///     * read the header row + row count via EPPlus
    ///     * insert a tbl_GMC_RolloverUpload row in status=Pending
    /// </summary>
    public class SalesUploadBL : ISalesUploadBL
    {
        private const string UploadRelativePath = "upload/GMCRollover";

        private readonly IRolloverUploadRepo _repo;
        private readonly IWebHostEnvironment _env;
        private readonly CommonBAL _commonBal;          // reused for industry list query
        private readonly IConfiguration _config;
        private readonly ISqlHelperQuery _sql;
        private readonly ILogger<SalesUploadBL> _log;

        public SalesUploadBL(IRolloverUploadRepo repo,
                             IWebHostEnvironment env,
                             CommonBAL commonBal,
                             IConfiguration config,
                             ISqlHelperQuery sql,
                             ILogger<SalesUploadBL> log)
        {
            _repo      = repo;
            _env       = env;
            _commonBal = commonBal;
            _config    = config;
            _sql       = sql;
            _log       = log;
        }

        public async Task<List<string>> GetIndustryListAsync()
        {
            return await FetchSingleColumnAsync(
                "SELECT [Nature of Industry] AS v FROM tbl_GMC_industry_master WITH (NOLOCK) ORDER BY [Nature of Industry]",
                "industry");
        }

        public Task<List<string>> GetCompanyListAsync(int top = 500)
        {
            // dbo.tbl_company_list is the same master used by the legacy uploader's
            // SearchInsuraceCompanyName endpoint.
            top = Math.Clamp(top, 1, 5000);
            return FetchSingleColumnAsync(
                $"SELECT TOP {top} company_name AS v FROM tbl_company_list WITH (NOLOCK) WHERE company_name IS NOT NULL AND LTRIM(RTRIM(company_name)) <> '' ORDER BY company_name",
                "company");
        }

        public Task<List<string>> GetTPAListAsync(int top = 500)
        {
            top = Math.Clamp(top, 1, 5000);
            return FetchSingleColumnAsync(
                $"SELECT TOP {top} TPA_Name AS v FROM tbl_TPA_list WITH (NOLOCK) WHERE TPA_Name IS NOT NULL AND LTRIM(RTRIM(TPA_Name)) <> '' ORDER BY TPA_Name",
                "tpa");
        }

        private Task<List<string>> FetchSingleColumnAsync(string sql, string label)
        {
            try
            {
                var cn = _config["ConnectionStrings:ConnectionToTele_Dashboard"];
                var dt = _sql.GetDataTable(cn, sql);
                var list = new List<string>();
                foreach (System.Data.DataRow r in dt.Rows)
                {
                    var v = r[0]?.ToString();
                    if (!string.IsNullOrWhiteSpace(v)) list.Add(v.Trim());
                }
                return Task.FromResult(list);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "{Label} master lookup failed; returning empty list.", label);
                return Task.FromResult(new List<string>());
            }
        }

        public async Task<SalesUploadResponse> UploadAsync(SalesUploadForm form, string uploadedBy)
        {
            var resp = new SalesUploadResponse();

            if (form.UploadFile == null || form.UploadFile.Length == 0)
            {
                resp.Message = "Please select an Excel (.xlsx) file.";
                return resp;
            }
            var ext = Path.GetExtension(form.UploadFile.FileName);
            if (!string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                resp.Message = "Only .xlsx files are accepted.";
                return resp;
            }
            if (string.IsNullOrWhiteSpace(form.PolicyNo))
            {
                resp.Message = "Policy number is required.";
                return resp;
            }

            // 1) save file under wwwroot/upload/GMCRollover/
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var safeName = Path.GetFileName(form.UploadFile.FileName);
            var fileName = $"{stamp}_{Guid.NewGuid():N}_{safeName}";
            var destDir  = Path.Combine(_env.WebRootPath, UploadRelativePath);
            Directory.CreateDirectory(destDir);
            var fullPath = Path.Combine(destDir, fileName);

            using (var fs = new FileStream(fullPath, FileMode.CreateNew))
                await form.UploadFile.CopyToAsync(fs);

            // 2) read header row + row count via EPPlus (no full data load)
            int totalRows = 0, totalCols = 0;
            try
            {
                using var pkg = new ExcelPackage(new FileInfo(fullPath));
                var ws = pkg.Workbook.Worksheets.FirstOrDefault();
                if (ws != null && ws.Dimension != null)
                {
                    totalRows = ws.Dimension.End.Row - ws.Dimension.Start.Row; // excluding header
                    totalCols = ws.Dimension.End.Column - ws.Dimension.Start.Column + 1;
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Could not read row/column count for {file}", fileName);
            }

            // 3) insert header row
            var entity = new RolloverUpload
            {
                PolicyNo         = form.PolicyNo.Trim(),
                PolicyName       = form.PolicyName?.Trim(),
                InsuranceCompany = form.InsuranceCompany?.Trim(),
                TPA              = form.TPA?.Trim(),
                IndustryName     = form.IndustryName?.Trim(),
                SubType          = form.SubType?.Trim(),
                DataCategory     = string.IsNullOrWhiteSpace(form.DataCategory) ? "Enrollment" : form.DataCategory,
                FileName         = safeName,
                FilePath         = Path.Combine(UploadRelativePath, fileName).Replace('\\', '/'),
                TotalRows        = totalRows,
                TotalColumns     = totalCols,
                Status           = UploadStatus.Pending,
                UploadedBy       = uploadedBy
            };
            var uploadId = await _repo.InsertUploadAsync(entity);

            resp.Success    = true;
            resp.UploadId   = uploadId;
            resp.Status     = UploadStatus.Pending;
            resp.Message    = $"Upload received. Reference #{uploadId}. The underwriter team will review it shortly.";
            resp.RedirectTo = $"/SalesUpload/Confirmation/{uploadId}";
            return resp;
        }
    }
}
