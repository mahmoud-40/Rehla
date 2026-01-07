using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class ForgetPasswordDTO
    {
        [EmailAddress]
        public string Email { get; set; }

        public string Code { get; set; }

        [SwaggerSchema(
            Description = "Password rules: Minimum 8 characters, at least one uppercase, one lowercase, one digit, one special character",
            Format = "Example@123"
        )]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Passwords Don't Match")]
        [DataType(DataType.Password)]
        [SwaggerSchema(
            Description = "Must match the password",
            Format = "Example@123"
        )]
        public string ConfirmPassword { get; set; }

    }
}