using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class PatientRegisterDTO : BaseRegisterDTO
    {
        [MaxLength(2000, ErrorMessage = "Medical history cannot exceed 2000 characters.")]
        public string? MedicalHistory { get; set; } 

        public override string Role => "Patient";
    }
}