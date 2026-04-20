using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class AssignCaregiverToPatientDTO
    {
        [Required]
        public string CaregiverId { get; set; } = string.Empty;

        [Required]
        public string PatientId { get; set; } = string.Empty;
    }
}

