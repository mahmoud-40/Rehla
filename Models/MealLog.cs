using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BreastCancer.Models
{
    public class MealLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int MealId { get; set; }

        [ForeignKey(nameof(MealId))]
        public required virtual NutritionMeal Meal { get; set; }

        [Required]
        public required string PatientId { get; set; }

        [ForeignKey(nameof(PatientId))]
        public required virtual Patient Patient { get; set; }

        public DateTime EatenAt { get; set; }
    }
}