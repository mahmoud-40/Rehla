using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class CaregiverRegisterDTO : BaseRegisterDTO
    {
        [MaxLength(50)]
        public string? RelationshipType { get; set; }
        [Required]
        public string PatientId { get; set; }
        public string Role => "Caregiver";

    }
}
