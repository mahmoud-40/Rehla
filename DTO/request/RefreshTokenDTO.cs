using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class RefreshTokenDTO
    {
        [Required] 
        public string RefreshToken { get; set; }
    }
}
