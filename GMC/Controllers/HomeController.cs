using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using GMC.Models;

namespace GMC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToAction("Login", "Account");

            var role = HttpContext.Session.GetString("UserRole");
            return role switch
            {
                "SalesPerson" => RedirectToAction("Upload",  "SalesUpload"),
                "Underwriter" => RedirectToAction("Pending", "Underwriter"),
                _              => RedirectToAction("Index",   "Dashboard")
            };
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}