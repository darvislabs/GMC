using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GMC.Helper
{
    public class SessionAuthFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();

            // Allow Account controller unconditionally (e.g. Login, Logout) and HomeController for start.
            if (controllerName == "Account" || controllerName == "Home" || string.IsNullOrEmpty(controllerName))
                return;

            // If session variable is empty, redirect to Account/Login
            var sessionUser = context.HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(sessionUser))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
