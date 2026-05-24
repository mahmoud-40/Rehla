using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BreastCancer.Enum;

namespace BreastCancer.Models
{
    public class NutritionMeal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int DayId { get; set; }

        [ForeignKey(nameof(DayId))]
        public required virtual NutritionPlanDay Day { get; set; }

        [Required]
        public MealType MealType { get; set; }

        [Required]
        public required string Name { get; set; }

        public int Calories { get; set; }

        public decimal Protein { get; set; }

        public decimal Carbs { get; set; }

        public decimal Fat { get; set; }

        public string? Benefits { get; set; }

        public string? Instructions { get; set; }

        public string? Notes { get; set; }

        public virtual ICollection<MealLog> MealLogs { get; set; } = new List<MealLog>();

    }
}