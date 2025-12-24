using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class LogoutDTO
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}
