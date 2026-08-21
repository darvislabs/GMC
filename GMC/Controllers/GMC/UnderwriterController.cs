using Microsoft.AspNetCore.Mvc;
using GMC.Helper;
using GMC.Interface.GMC;
using GMC.Models.GMC;

namespace GMC.Controllers.GMC
{
    /// <summary>
    /// Underwriter team workspace.
    ///     /Underwriter/Pending    — queue of uploads waiting for AI mapping / approval
    ///     /Underwriter/Review/n   — single upload + mapping editor with AI assist
    ///     /Underwriter/Save       — persist mapping (Approve=true → ready for calculator)
    /// </summary>
    [RoleAuth("Underwriter")]
    public class UnderwriterController : Controller
    {
        private readonly IUnderwriterBL _bl;
        private readonly ILogger<UnderwriterController> _log;

        public UnderwriterController(IUnderwriterBL bl, ILogger<UnderwriterController> log)
        {
            _bl  = bl;
            _log = log;
        }

        private (string role, string userName) Identity()
            => (HttpContext.Session.GetString("UserRole") ?? string.Empty,
                HttpContext.Session.GetString("UserName") ?? string.Empty);

        [HttpGet]
        public async Task<IActionResult> Pending()
        {
            var (role, user) = Identity();
            // "Pending" tab shows uploads that haven't been completed yet
            var list = (await _bl.GetQueueAsync(role, user))
                .Where(u => u.Status != UploadStatus.Completed && u.Status != UploadStatus.Rejected)
                .ToList();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> All()
        {
            var (role, user) = Identity();
            var list = await _bl.GetQueueAsync(role, user);
            return View("Pending", list);  // reuse same grid view
        }

        [HttpGet]
        public async Task<IActionResult> Review(int id, bool rematch = false)
        {
            try
            {
                var (_, user) = Identity();
                var vm = await _bl.GetReviewAsync(id, forceRematch: rematch);

                // First time an underwriter opens it: mark UnderReview & assign self
                if (vm.Upload.Status == UploadStatus.Pending ||
                    vm.Upload.Status == UploadStatus.MappingRequired)
                {
                    // status already moved by GetReviewAsync; just stamp the reviewer
                }
                return View(vm);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Review failed for upload {Id}", id);
                TempData["UploadError"] = "Could not open the upload: " + ex.Message;
                return RedirectToAction(nameof(Pending));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save([FromBody] SaveMappingRequest req)
        {
            if (req == null) return BadRequest("No payload.");
            var (_, user) = Identity();

            try
            {
                var saved = await _bl.SaveMappingAsync(req, user);
                string? nextUrl = null;
                string? nextMessage = null;
                bool openCalculator = false;

                if (req.Approve && saved != null)
                {
                    var nav = await _bl.GetPostApproveRedirectAsync(saved.PolicyNo);
                    nextMessage = nav.Message;
                    openCalculator = nav.OpenCalculator;

                    if (nav.OpenCalculator)
                    {
                        nextUrl = Url.Action("GMCCalculatorpremium", "GMCCalculatorDetails",
                            new { policyno = saved.PolicyNo });
                    }
                    else if (nav.ReviewUploadId.HasValue)
                    {
                        nextUrl = Url.Action("Review", "Underwriter",
                            new { id = nav.ReviewUploadId.Value });
                    }
                }
                else if (saved != null)
                {
                    nextUrl = Url.Action("Review", "Underwriter", new { id = saved.UploadId });
                }

                return Json(new
                {
                    success        = true,
                    uploadId       = saved?.UploadId,
                    status         = saved?.Status,
                    confidence     = saved?.MappingConfidenceAvg,
                    approved       = req.Approve,
                    nextUrl,
                    nextMessage,
                    openCalculator
                });
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "SaveMapping failed for upload {Id}", req.UploadId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var (_, user) = Identity();
            await _bl.RejectAsync(id, reason ?? "(no reason given)", user);
            TempData["UploadSuccess"] = $"Upload #{id} rejected.";
            return RedirectToAction(nameof(Pending));
        }

        /// <summary>
        /// Marks an upload as Completed.  Intended to be called by the
        /// calculator save flow once the rollover quote is finalised.
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]   // allows server-side or AJAX-from-calculator calls
        public async Task<IActionResult> MarkCompleted(int id)
        {
            var (_, user) = Identity();
            try
            {
                await _bl.SaveMappingAsync(new SaveMappingRequest
                {
                    UploadId = id,
                    Mappings = new(),     // not changing the mapping; just status
                    Approve  = false,
                    Remarks  = "Completed via calculator."
                }, user);
            }
            catch { /* mapping save may noop if empty; we still want status update */ }

            // explicit status transition
            var repoField = HttpContext.RequestServices
                .GetService(typeof(IRolloverUploadRepo)) as IRolloverUploadRepo;
            if (repoField != null)
            {
                await repoField.UpdateStatusAsync(id, UploadStatus.Completed, user);
            }
            return Json(new { success = true, status = UploadStatus.Completed });
        }
    }
}
