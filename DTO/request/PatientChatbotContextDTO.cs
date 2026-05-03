using System.Text.Json.Serialization;

namespace BreastCancer.DTO.request
{
    public class PatientChatbotContextDTO
    {
        [JsonPropertyName("age_at_diagnosis")]
        public int AgeAtDiagnosis { get; set; }

        [JsonPropertyName("cancer_type")]
        public string CancerType { get; set; }

        [JsonPropertyName("cancer_type_detailed")]
        public string CancerTypeDetailed { get; set; }

        [JsonPropertyName("tumor_stage")]
        public string TumorStage { get; set; }

        [JsonPropertyName("neoplasm_histologic_grade")]
        public string NeoplasmHistologicGrade { get; set; }

        [JsonPropertyName("er_status")]
        public string ErStatus { get; set; }

        [JsonPropertyName("pr_status")]
        public string PrStatus { get; set; }

        [JsonPropertyName("her2_status")]
        public string Her2Status { get; set; }

        [JsonPropertyName("chemotherapy")]
        public bool Chemotherapy { get; set; }

        [JsonPropertyName("hormone_therapy")]
        public bool HormoneTherapy { get; set; }

        [JsonPropertyName("radio_therapy")]
        public bool RadioTherapy { get; set; }
    }
}

