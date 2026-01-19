using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BreastCancer.Enum;

namespace BreastCancer.Models
{
    public class Caregiver
    {
        [Key]
        public string UserId { get; set; }
        public RelationshipType? RelationshipType { get; set; }
        [Required]
        public string PatientId { get; set; }
        public virtual Patient Patient { get; set; }

        public virtual ApplicationUser User { get; set; }

    }
}