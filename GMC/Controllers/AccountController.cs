using Microsoft.AspNetCore.Mvc;
using GMC.Models.GMC;
using GMC.Interface.GMC;
using GMC.Models.GMC.BusinessLogic;
using GMC.DAL.Repository.GMC;

namespace GMC.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILoginBL _loginBL;
        private readonly IUserRegistrationBL _registrationBL;
        private readonly MasterDAL _masterDal;

        public AccountController(ILoginBL loginBL, IUserRegistrationBL registrationBL, MasterDAL masterDal)
        {
            _loginBL = loginBL;
            _registrationBL = registrationBL;
            _masterDal = masterDal;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                string? role = await _loginBL.ValidateUserAndGetRole(model);
                if (!string.IsNullOrEmpty(role))
                {
                    HttpContext.Session.SetString("UserName", model.Username);
                    HttpContext.Session.SetString("UserRole", role);
                    return role switch
                    {
                        "SalesPerson" => RedirectToAction("Upload", "SalesUpload"),
                        "Underwriter" => RedirectToAction("Pending", "Underwriter"),
                        _              => RedirectToAction("Index", "Dashboard")
                    };
                }
                else
                {
                    ViewBag.Error = "Invalid Username or Password";
                }
            }
            return View(model);
        }
        
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                bool exists = await _loginBL.IsEmailRegistered(model.EmailAddress);
                if (exists)
                {
                    // Generate mock OTP
                    string otp = new Random().Next(100000, 999999).ToString();
                    HttpContext.Session.SetString("ResetOTP_" + model.EmailAddress, otp);
                    
                    // Display OTP on next page (since no actual SMTP setup)
                    TempData["MockEmailMessage"] = $"Mock Email Sent! Your passcode is: {otp}";
                    
                    return RedirectToAction("VerifyCode", new { email = model.EmailAddress });
                }
                ViewBag.Error = "Email address not found.";
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult VerifyCode(string email)
        {
            if (string.IsNullOrEmpty(email)) return RedirectToAction("ForgotPassword");
            return View(new VerifyCodeModel { EmailAddress = email });
        }

        [HttpPost]
        public IActionResult VerifyCode(VerifyCodeModel model)
        {
            if (ModelState.IsValid)
            {
                string sessionOtp = HttpContext.Session.GetString("ResetOTP_" + model.EmailAddress);
                if (!string.IsNullOrEmpty(sessionOtp) && sessionOtp == model.Code)
                {
                    // Mark as verified for reset stage
                    HttpContext.Session.SetString("ResetVerified_" + model.EmailAddress, "true");
                    return RedirectToAction("ResetPassword", new { email = model.EmailAddress });
                }
                ViewBag.Error = "Invalid or expired passcode.";
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email) || HttpContext.Session.GetString("ResetVerified_" + email) != "true")
            {
                return RedirectToAction("ForgotPassword");
            }
            return View(new ResetPasswordModel { EmailAddress = email });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.NewPassword != model.ConfirmPassword)
                {
                    ViewBag.Error = "Passwords do not match.";
                    return View(model);
                }

                string verified = HttpContext.Session.GetString("ResetVerified_" + model.EmailAddress);
                if (verified == "true")
                {
                    bool success = await _loginBL.UpdatePassword(model.EmailAddress, model.NewPassword);
                    if (success)
                    {
                        HttpContext.Session.Remove("ResetVerified_" + model.EmailAddress);
                        HttpContext.Session.Remove("ResetOTP_" + model.EmailAddress);
                        TempData["SuccessMessage"] = "Password reset successfully. Please log in.";
                        return RedirectToAction("Login");
                    }
                    ViewBag.Error = "Failed to update password.";
                }
                else
                {
                    return RedirectToAction("ForgotPassword");
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult UserRegistration()
        {
            ViewBag.Roles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_masterDal.GetUserRoles(), "RoleName", "RoleName");
            return View();
        }

        [HttpPost]
        public IActionResult UserRegistration(UserRegistrationModel model)
        {
            ViewBag.Roles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_masterDal.GetUserRoles(), "RoleName", "RoleName");
            if (ModelState.IsValid)
            {
                var sessionUser = HttpContext.Session.GetString("UserName");
                model.createdby = !string.IsNullOrEmpty(sessionUser) ? sessionUser : "System";

                bool isSuccess = _registrationBL.RegisterUser(model, out var errorMessage);
                if (isSuccess)
                {
                    ViewBag.Success = "User registered successfully!";
                    ModelState.Clear();
                    return View();
                }
                else
                {
                    ViewBag.Error = string.IsNullOrWhiteSpace(errorMessage)
                        ? "An error occurred during registration."
                        : errorMessage;
                }
            }
            return View(model);
        }
    }
}
