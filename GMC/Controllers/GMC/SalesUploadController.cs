using Microsoft.AspNetCore.Mvc;
using GMC.Helper;
using GMC.Interface.GMC;
using GMC.Models.GMC;

namespace GMC.Controllers.GMC
{
    /// <summary>
    /// Sales-Person entry point.  Single responsibility: accept an .xlsx
    /// rollover file, persist it, and create a tbl_GMC_RolloverUpload row in
    /// status=Pending for the underwriter team to pick up.
    /// </summary>
    [RoleAuth("SalesPerson")]
    public class SalesUploadController : Controller
    {
        private readonly ISalesUploadBL _bl;
        private readonly IRolloverUploadRepo _repo;
        private readonly IGMCUploader _gmc;
        private readonly ILogger<SalesUploadController> _log;

        public SalesUploadController(ISalesUploadBL bl,
                                     IRolloverUploadRepo repo,
                                     IGMCUploader gmc,
                                     ILogger<SalesUploadController> log)
        {
            _bl   = bl;
            _repo = repo;
            _gmc  = gmc;
            _log  = log;
        }

        /// <summary>JSON autocomplete from dbo.tbl_company_list.</summary>
        [HttpGet, HttpPost]
        public IActionResult SearchCompany(string prefixText)
        {
            try
            {
                var list = _gmc.getSearchInsuraceCompanyName(prefixText ?? string.Empty);
                return Json(list);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Company autocomplete failed for prefix '{Prefix}'", prefixText);
                return Json(Array.Empty<string>());
            }
        }

        /// <summary>JSON autocomplete from dbo.tbl_TPA_list.</summary>
        [HttpGet, HttpPost]
        public IActionResult SearchTPA(string prefixText)
        {
            try
            {
                var list = _gmc.getSearchTPA(prefixText ?? string.Empty);
                return Json(list);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "TPA autocomplete failed for prefix '{Prefix}'", prefixText);
                return Json(Array.Empty<string>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Upload()
        {
            var vm = new SalesUploadForm();
            await PopulateMastersAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(ValueCountLimit = int.MaxValue)]
        [RequestSizeLimit(200 * 1024 * 1024)]   // 200 MB (two .xlsx files now)
        public async Task<IActionResult> Upload(SalesUploadForm form)
        {
            // Both Enrollment and Claim files are required.
            bool hasEnroll = form.EnrollmentFile != null && form.EnrollmentFile.Length > 0;
            bool hasClaim  = form.ClaimFile      != null && form.ClaimFile.Length      > 0;

            if (!hasEnroll)
                ModelState.AddModelError(nameof(SalesUploadForm.EnrollmentFile), "Enrollment Excel is required.");
            if (!hasClaim)
                ModelState.AddModelError(nameof(SalesUploadForm.ClaimFile), "Claim Excel is required.");

            if (!ModelState.IsValid)
            {
                await PopulateMastersAsync(form);
                return View(form);
            }

            var uploadedBy = HttpContext.Session.GetString("UserName") ?? "unknown";
            var done = new List<(string category, SalesUploadResponse resp)>();
            var failed = new List<string>();

            // -------- enrollment --------
            var enrollForm = CloneFormFor(form, form.EnrollmentFile!, "Enrollment");
            var enrollResp = await _bl.UploadAsync(enrollForm, uploadedBy);
            if (enrollResp.Success) done.Add(("Enrollment", enrollResp));
            else failed.Add($"Enrollment: {enrollResp.Message}");

            // -------- claim --------
            var claimForm = CloneFormFor(form, form.ClaimFile!, "Claim");
            var claimResp = await _bl.UploadAsync(claimForm, uploadedBy);
            if (claimResp.Success) done.Add(("Claim", claimResp));
            else failed.Add($"Claim: {claimResp.Message}");

            if (done.Count != 2)
            {
                TempData["UploadError"] = failed.Count > 0
                    ? string.Join(" | ", failed)
                    : "Both Enrollment and Claim files must upload successfully.";
                await PopulateMastersAsync(form);
                return View(form);
            }

            var enrollId = done.First(d => d.category == "Enrollment").resp.UploadId;
            var claimId  = done.First(d => d.category == "Claim").resp.UploadId;
            TempData["UploadSuccess"] = $"Enrollment and Claim data uploaded successfully for policy {form.PolicyNo}. " +
                                         $"Refs: Enrollment #{enrollId}, Claim #{claimId}.";
            return RedirectToAction(nameof(Confirmation), new { id = enrollId });
        }

        /// <summary>
        /// Builds a per-file copy of the form so the BL's existing single-file
        /// pipeline can run twice (once per Excel) without us duplicating its
        /// logic here.
        /// </summary>
        private static SalesUploadForm CloneFormFor(SalesUploadForm src, IFormFile file, string category)
        {
            return new SalesUploadForm
            {
                PolicyNo         = src.PolicyNo,
                PolicyName       = src.PolicyName,
                InsuranceCompany = src.InsuranceCompany,
                TPA              = src.TPA,
                IndustryName     = src.IndustryName,
                SubType          = src.SubType,
                DataCategory     = category,
                UploadFile       = file
            };
        }

        private async Task PopulateMastersAsync(SalesUploadForm vm)
        {
            // Fetch all three masters in parallel so the form loads fast even on
            // a slow DB connection.
            var industryTask = _bl.GetIndustryListAsync();
            var companyTask  = _bl.GetCompanyListAsync();
            var tpaTask      = _bl.GetTPAListAsync();
            await Task.WhenAll(industryTask, companyTask, tpaTask);

            vm.IndustryOptions = industryTask.Result;
            vm.CompanyOptions  = companyTask.Result;
            vm.TpaOptions      = tpaTask.Result;
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var upload = await _repo.GetUploadByIdAsync(id);
            if (upload == null)
            {
                TempData["UploadError"] = "Upload reference not found.";
                return RedirectToAction(nameof(Upload));
            }

            // Sales-person should only see their own uploads
            var userName = HttpContext.Session.GetString("UserName");
            if (!string.Equals(upload.UploadedBy, userName, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            return View(upload);
        }

        [HttpGet]
        public async Task<IActionResult> MyUploads()
        {
            var role     = HttpContext.Session.GetString("UserRole") ?? "SalesPerson";
            var userName = HttpContext.Session.GetString("UserName") ?? string.Empty;
            var list = await _repo.GetPendingUploadsAsync(role, userName);
            return View(list);
        }
    }
}
