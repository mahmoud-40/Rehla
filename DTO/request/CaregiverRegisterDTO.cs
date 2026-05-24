using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class CaregiverRegisterDTO : BaseRegisterDTO
    {
        [Required(ErrorMessage = "Relationship type is required.")]
        [MaxLength(50, ErrorMessage = "Relationship type cannot exceed 50 characters.")]
        public string RelationshipType { get; set; }

        [Required(ErrorMessage = "Patient email is required.")]
        [EmailAddress(ErrorMessage = "Invalid patient email format.")]
        public string PatientEmail { get; set; }

        public override string Role => "Caregiver";
    }
}