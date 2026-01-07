using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class ResetPasswordDTO
    {
        [EmailAddress]
        public string Email { get; set; }
       
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }
       
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Passwords Don't Match")]
        public string ConfirmPassword { get; set; }
    }
}
