using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class MedicineCreateDTO
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Instructions { get; set; } = string.Empty;

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        [Range(1, 24)]
        public int IntervalHours { get; set; }
    }
}

