using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BreastCancer.Models
{
    public class Patient
    {
        [Key]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; }

        [MaxLength(2000)]
        public string? MedicalHistory { get; set; } // TODO: Consider making this a separate entity
        public string? DoctorId { get; set; }
        public virtual TreatmentPlan? TreatmentPlan { get; set; }
        public virtual Doctor? Doctor { get; set; }
        public virtual ICollection<NutritionPlan> NutritionPlans { get; set; } = new List<NutritionPlan>();
        public virtual ICollection<MealLog> MealLogs { get; set; } = new List<MealLog>();

        public virtual ApplicationUser? User { get; set; }

        public virtual ICollection<Caregiver> Caregivers { get; set; } = new List<Caregiver>();
    }
}