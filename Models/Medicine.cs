using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BreastCancer.Models
{
    public class Medicine
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Instruction { get; set; }

        [Required]
        public DateTime StartTime { get; set; }
        
        [Required]
        public int IntervalHours { get; set; } // Hours between doses
        
        public DateTime? EndTime { get; set; }
        
        public DateTime? LastTaken { get; set; }
        
        public DateTime? NextAlert { get; set; } // Calculated next alert time

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        [Required]
        public int TreatmentPlanId { get; set; }
        public virtual TreatmentPlan TreatmentPlan { get; set; }    
    }
}
