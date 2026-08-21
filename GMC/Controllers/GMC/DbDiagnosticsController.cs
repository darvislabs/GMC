using GMC.BL.GMC;
using GMC.Helper;
using Microsoft.AspNetCore.Mvc;

namespace GMC.Controllers.GMC
{
    [RoleAuth("Admin")]
    public class DbDiagnosticsController : Controller
    {
        private readonly IDbDiagnosticsBL _bl;

        public DbDiagnosticsController(IDbDiagnosticsBL bl) => _bl = bl;

        public async Task<IActionResult> Index(string? schema = "dbo")
        {
            ViewData["Title"] = "DB Diagnostics";
            var vm = await _bl.BuildAsync(schema);
            return View(vm);
        }
    }
}

