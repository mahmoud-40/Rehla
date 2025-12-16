using System.ComponentModel.DataAnnotations;

namespace BreastCancer.Models
{
    public class Doctor : ApplicationUser
    {
        [MaxLength(100)]
        public string? Specialization { get; set; }

        [MaxLength(50)]
        public string? LicenseNumber { get; set; }

        [Range(0, 60)]
        public int? YearsOfExperience { get; set; }

        public bool IsVerified { get; set; } = false;


        public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
    }
}