using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BreastCancer.Models
{
    public class Caregiver
    {
        [Key]
        public string UserId { get; set; }

        [MaxLength(50)]
        public string? RelationshipType { get; set; }
        [Required]
        public string PatientId { get; set; }
        public virtual Patient Patient { get; set; }

        public virtual ApplicationUser User { get; set; }

    }
}