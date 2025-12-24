using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class LoginDTO
    {
        [EmailAddress]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
