using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class ChatbotAskDTO
    {
        [Required(ErrorMessage = "Question is required")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Question must be between 1 and 1000 characters")]
        public string Question { get; set; }

        [Required(ErrorMessage = "PatientId is required")]
        [StringLength(450, MinimumLength = 1, ErrorMessage = "Invalid PatientId")]
        public string PatientId { get; set; }
    }
}
