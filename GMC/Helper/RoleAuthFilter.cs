using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GMC.Helper
{
    /// <summary>
    /// Use on a controller or action to restrict access to specific roles.
    /// Requires <see cref="SessionAuthFilter"/> to be installed globally — relies
    /// on Session["UserName"] + Session["UserRole"] which the Account/Login flow sets.
    ///
    /// Example:
    ///     [RoleAuth("SalesPerson", "Admin")]
    ///     public class SalesUploadController : Controller { ... }
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RoleAuthAttribute : Attribute, IActionFilter
    {
        private readonly string[] _allowedRoles;

        public RoleAuthAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles ?? Array.Empty<string>();
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var userName = session.GetString("UserName");
            var userRole = session.GetString("UserRole");

            if (string.IsNullOrEmpty(userName))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (_allowedRoles.Length > 0 &&
                !_allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase) &&
                !string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new ViewResult { ViewName = "AccessDenied" };
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
