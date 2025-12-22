using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BreastCancer.Models
{
    public class Doctor
    {
        [Key]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; }

        [MaxLength(100)]
        public string? Specialization { get; set; }

        [MaxLength(50)]
        public string? LicenseNumber { get; set; }

        [Range(0, 60)]
        public int? YearsOfExperience { get; set; }

        public bool IsVerified { get; set; } = false;

        public virtual ApplicationUser User { get; set; }

        public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
    }
}