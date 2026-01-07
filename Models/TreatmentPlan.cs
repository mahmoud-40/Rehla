using BreastCancer.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BreastCancer.Models
{
    public class TreatmentPlan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public TreatmentPlanStatus Status { get; set; } = TreatmentPlanStatus.NotStarted;

        [Required]
        public string PatientId { get; set; }
        public virtual Patient Patient { get; set; }
        
        public string? DoctorId { get; set; } // Optional - patient can manually input doctor name
        public virtual Doctor? Doctor { get; set; }
        
        [MaxLength(100)]
        public string? DoctorName { get; set; } // For manual entry when DoctorId is null

        public virtual ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
        public virtual ICollection<TreatmentPlanHistory> History { get; set; } = new List<TreatmentPlanHistory>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
