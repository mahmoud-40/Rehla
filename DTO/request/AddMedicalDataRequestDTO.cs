using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;


namespace BreastCancer.DTO.request
{
    public class AddMedicalDataRequestDTO
    {
        [JsonPropertyName("patient_id")]
        [NotEmptyGuid(ErrorMessage = "Patient ID is required.")]
        public Guid PatientId { get; set; }

        [JsonPropertyName("patient_context")]
        [Required(ErrorMessage = "Patient context is required.")]
        public required PatientContext Context { get; set; }
    }

    public class PatientContext
    {
        [JsonPropertyName("age_at_diagnosis")]
        [Range(1, 130, ErrorMessage = "Age at diagnosis must be between 1 and 130.")]
        public int AgeAtDiagnosis { get; set; }

        [JsonPropertyName("cancer_type")]
        [Required(ErrorMessage = "Cancer type is required.")]
        [StringLength(100, ErrorMessage = "Cancer type cannot exceed 100 characters.")]
        public required string CancerType { get; set; }

        [JsonPropertyName("cancer_type_detailed")]
        [Required(ErrorMessage = "Detailed cancer type is required.")]
        [StringLength(200, ErrorMessage = "Detailed cancer type cannot exceed 200 characters.")]
        public required string CancerTypeDetailed { get; set; }

        [JsonPropertyName("tumor_stage")]
        [Required(ErrorMessage = "Tumor stage is required.")]
        [StringLength(50, ErrorMessage = "Tumor stage cannot exceed 50 characters.")]
        public required string TumorStage { get; set; }

        [JsonPropertyName("neoplasm_histologic_grade")]
        [Required(ErrorMessage = "Neoplasm histologic grade is required.")]
        [StringLength(100, ErrorMessage = "Neoplasm histologic grade cannot exceed 100 characters.")]
        public required string NeoplasmHistologicGrade { get; set; }

        [JsonPropertyName("er_status")]
        [Required(ErrorMessage = "ER status is required.")]
        [StringLength(50, ErrorMessage = "ER status cannot exceed 50 characters.")]
        public required string ErStatus { get; set; }

        [JsonPropertyName("pr_status")]
        [Required(ErrorMessage = "PR status is required.")]
        [StringLength(50, ErrorMessage = "PR status cannot exceed 50 characters.")]
        public required string PrStatus { get; set; }

        [JsonPropertyName("her2_status")]
        [Required(ErrorMessage = "HER2 status is required.")]
        [StringLength(50, ErrorMessage = "HER2 status cannot exceed 50 characters.")]
        public required string Her2Status { get; set; }

        [JsonPropertyName("chemotherapy")]
        [Required(ErrorMessage = "Chemotherapy is required.")]
        public bool? Chemotherapy { get; set; }

        [JsonPropertyName("hormone_therapy")]
        [Required(ErrorMessage = "Hormone therapy is required.")]
        public bool? HormoneTherapy { get; set; }

        [JsonPropertyName("radio_therapy")]
        [Required(ErrorMessage = "Radio therapy is required.")]
        public bool? RadioTherapy { get; set; }
    }

    public sealed class NotEmptyGuidAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is Guid guid)
            {
                return guid != Guid.Empty;
            }

            return false;
        }
    }
}