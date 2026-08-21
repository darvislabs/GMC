using System.ComponentModel.DataAnnotations;

namespace GMC.Models.GMC
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }
    }

    public class ForgotPasswordModel
    {
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string EmailAddress { get; set; }
    }

    public class VerifyCodeModel
    {
        [Required]
        public string EmailAddress { get; set; }

        [Required(ErrorMessage = "Verification code is required")]
        public string Code { get; set; }
    }

    public class ResetPasswordModel
    {
        [Required]
        public string EmailAddress { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Please confirm your password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }
}
