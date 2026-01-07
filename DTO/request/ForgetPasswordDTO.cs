using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class ForgetPasswordDTO
    {
        [EmailAddress]
        public string Email { get; set; }

        public string Code { get; set; }

        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Passwords Don't Match")]
        public string ConfirmPassword { get; set; }

    }
}