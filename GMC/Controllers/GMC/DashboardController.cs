using Microsoft.AspNetCore.Mvc;
using GMC.Interface.GMC;

namespace GMC.Controllers.GMC
{
    /// <summary>
    /// Role-aware dashboard. Visible to any logged-in user; the data is
    /// already filtered server-side by role inside the SPs.
    /// </summary>
    public class DashboardController : Controller
    {
        private readonly IDashboardBL _bl;
        public DashboardController(IDashboardBL bl) => _bl = bl;

        public async Task<IActionResult> Index()
        {
            var role     = HttpContext.Session.GetString("UserRole") ?? string.Empty;
            var userName = HttpContext.Session.GetString("UserName") ?? string.Empty;
            if (string.IsNullOrEmpty(userName))
                return RedirectToAction("Login", "Account");

            var vm = await _bl.BuildAsync(role, userName);
            return View(vm);
        }
    }
}
