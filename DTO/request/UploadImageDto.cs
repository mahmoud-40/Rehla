using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class UploadImageDto
    {
        [Required]
        public IFormFile File { get; set; }
    }
}
