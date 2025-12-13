using System.ComponentModel.DataAnnotations;

namespace BreastCancer.Models
{
    public class Caregiver : User
    {
        [MaxLength(50)]
        public string? RelationshipType { get; set; }
        [Required]
        public string PatientId { get; set; }
        public virtual Patient Patient { get; set; }
    }
}