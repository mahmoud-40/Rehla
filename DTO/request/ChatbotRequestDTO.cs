using System.Text.Json.Serialization;

namespace BreastCancer.DTO.request
{
    public class ChatbotRequestDTO
    {
        [JsonPropertyName("question")]
        public string Question { get; set; }

        [JsonPropertyName("patient_context")]
        public PatientChatbotContextDTO PatientContext { get; set; }
    }
}