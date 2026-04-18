using BreastCancer.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BreastCancer.Models
{
    public class NutritionPlan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public required string PatientId { get; set; }

        [ForeignKey("PatientId")]
        public required virtual Patient Patient { get; set; }

        [Required]
        public required string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public NutritionPlanStatus Status { get; set; } = NutritionPlanStatus.Draft;

        public NutritionPlanSource Source { get; set; } = NutritionPlanSource.Manual;

        public bool IsLocked { get; set; }

        public string? DoctorId { get; set; }

        public virtual Doctor? Doctor { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public string? ApprovedBy { get; set; }

        public string? RejectionNote { get; set; }

        public virtual ICollection<NutritionPlanDay> Days { get; set; } = new List<NutritionPlanDay>();
    }
}
