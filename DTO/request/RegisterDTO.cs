using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class RegisterDTO
    {
        [MinLength(3)]
        public string FirstName { get; set; }

        [MinLength(3)]
        public string LastName { get; set; }

        [MinLength(3)]
        public string Username { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [RegularExpression("^(Patient|Doctor|Caregiver)$",
        ErrorMessage = "Role must be Patient, Doctor, or Caregiver")]
        public string Role { get; set; }

        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Passwords Don't Match")]
        public string ConfirmPassword { get; set; }
    }
}
