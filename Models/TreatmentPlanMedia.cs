using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BreastCancer.Models
{
    public class TreatmentPlanMedia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string MediaUrl { get; set; }

        [Required]
        [MaxLength(200)]
        public string MediaType { get; set; }

        [Required]
        public int TreatmentPlanHistoryId { get; set; }
        public virtual TreatmentPlanHistory TreatmentPlanHistory { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
