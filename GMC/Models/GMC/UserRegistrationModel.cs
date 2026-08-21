using System.ComponentModel.DataAnnotations;

namespace GMC.Models.GMC
{
    public class UserRegistrationModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email ID is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [Display(Name = "Email ID")]
        public string emailid { get; set; }

        [Required(ErrorMessage = "Mobile number is required")]
        [Phone]
        [Display(Name = "Mobile Number")]
        public string mobile { get; set; }

        [Required(ErrorMessage = "Address is required")]
        public string address { get; set; }

        [Required(ErrorMessage = "User Type is required")]
        [Display(Name = "User Type")]
        public string usertype { get; set; }
        
        public DateTime? createdon { get; set; }
        public string? createdby { get; set; }
    }
}
