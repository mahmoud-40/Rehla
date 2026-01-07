using BreastCancer.Enum;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BreastCancer.DTO.request
{
    public abstract class BaseRegisterDTO
    {
        [MinLength(3)]
        public string FirstName { get; set; }

        [MinLength(3)]
        public string LastName { get; set; }

        [MinLength(3)]
        public string Username { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        [SwaggerSchema(
            Description = "Enter your email",
            Format = "user@gmail.com"
        )]
        [EmailAddress]
        public string Email { get; set; }
        public string Address { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public Gender Gender { get; set; }

        public virtual string Role { get; set; }

        [JsonIgnore]
        public string? EmailConfirmationCode { get; set; }

        [JsonIgnore]
        public DateTime? EmailConfirmationCodeExpiresAt { get; set; }

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
