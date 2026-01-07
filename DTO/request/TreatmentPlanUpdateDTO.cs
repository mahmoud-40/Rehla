using BreastCancer.Enum;
using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class TreatmentPlanUpdateDTO
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? DoctorName { get; set; }

        public string? DoctorId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public TreatmentPlanStatus? Status { get; set; }

        public List<MedicineUpdateDTO>? Medicines { get; set; }

        public string? PrescriptionImageUrl { get; set; }
    }
}

