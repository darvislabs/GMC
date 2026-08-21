using Microsoft.AspNetCore.Mvc;
using global::GMC.Models.GMC;
using global::GMC.DAL.Repository.GMC;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GMC.Controllers
{
    public class MasterController : Controller
    {
        private readonly MasterDAL _masterDal;

        public MasterController(MasterDAL masterDal)
        {
            _masterDal = masterDal;
        }

        [HttpGet]
        public IActionResult UserRole()
        {
            var roles = _masterDal.GetUserRoles();
            return View(roles);
        }

        [HttpPost]
        public IActionResult UserRole(string roleName)
        {
            if (!string.IsNullOrEmpty(roleName))
            {
                _masterDal.AddUserRole(roleName);
            }
            return RedirectToAction("UserRole");
        }

        [HttpGet]
        public IActionResult FormPermission()
        {
            ViewBag.Roles = new SelectList(_masterDal.GetUserRoles(), "RoleId", "RoleName");
            var permissions = _masterDal.GetPermissions();
            return View(permissions);
        }

        [HttpPost]
        public IActionResult FormPermission(FormPermissionModel perm)
        {
            if (ModelState.IsValid)
            {
                _masterDal.AddFormPermission(perm);
            }
            return RedirectToAction("FormPermission");
        }
    }
}
