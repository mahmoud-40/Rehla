using BreastCancer.Enum;

namespace BreastCancer.DTO.response
{
    public class TreatmentPlanResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? DoctorName { get; set; }
        public string? DoctorId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public TreatmentPlanStatus Status { get; set; }
        public string PatientId { get; set; } = string.Empty;
        public List<MedicineResponseDTO> Medicines { get; set; } = new List<MedicineResponseDTO>();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

