using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class AssignDoctorToPatientDTO
    {
        [Required]
        public string DoctorId { get; set; } = string.Empty;

        [Required]
        public string PatientId { get; set; } = string.Empty;
    }
}

