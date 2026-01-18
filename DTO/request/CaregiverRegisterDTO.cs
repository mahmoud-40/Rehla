using System.ComponentModel.DataAnnotations;
using BreastCancer.Enum;

namespace BreastCancer.DTO.request
{
    public class CaregiverRegisterDTO : BaseRegisterDTO
    {
        public RelationshipType? RelationshipType { get; set; }
        [Required]
        public string PatientId { get; set; }
        public override string Role => "Caregiver";

    }
}
