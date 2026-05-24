using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class AdminDeleteUserDTO
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string UserType { get; set; } = string.Empty;
    }
}

