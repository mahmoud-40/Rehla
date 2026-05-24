using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class AdminDisableUserDTO
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
    }
}

