using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BreastCancer.Models
{
    public class NutritionPlanDay
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int PlanId { get; set; }

        [ForeignKey(nameof(PlanId))]
        public required virtual NutritionPlan Plan { get; set; }

        [Range(1, 7)]
        public int DayNumber { get; set; }

        public virtual ICollection<NutritionMeal> Meals { get; set; } = new List<NutritionMeal>();
    }
}