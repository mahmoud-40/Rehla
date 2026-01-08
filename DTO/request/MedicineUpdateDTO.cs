using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class MedicineUpdateDTO
    {
        public int? Id { get; set; } // If provided, update existing medicine; if null, create new

        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(500)]
        public string? Instructions { get; set; }

        public DateTime? StartTime { get; set; }

        [Range(1, 24)]
        public int? IntervalHours { get; set; }

        public DateTime? EndTime { get; set; }

        public DateTime? LastTaken { get; set; } // When medicine is marked as taken - will trigger NextAlert recalculation
    }
}

