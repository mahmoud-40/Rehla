using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.response
{
    public class ChatbotResponse
    {
        [Required]
        public string Answer { get; set; } = string.Empty;
    }
}
