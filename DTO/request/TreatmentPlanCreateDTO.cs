using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class TreatmentPlanCreateDTO
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? DoctorName { get; set; }

        public string? DoctorId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one medicine is required")]
        public List<MedicineCreateDTO> Medicines { get; set; } = new List<MedicineCreateDTO>();

        public string? PrescriptionImageUrl { get; set; } // For future file upload support
    }
}

