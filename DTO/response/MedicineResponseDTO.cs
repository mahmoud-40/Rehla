namespace BreastCancer.DTO.response
{
    public class MedicineResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public DateTime StartTime { get; set; }
        public int IntervalHours { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime? LastTaken { get; set; }
        public DateTime? NextAlert { get; set; }
        public int TreatmentPlanId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

