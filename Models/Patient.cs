using System.ComponentModel.DataAnnotations;

namespace BreastCancer.Models
{
    public class Patient : ApplicationUser
    {
        [MaxLength(2000)]
        public string? MedicalHistory { get; set; } // TODO: Consider making this a separate entity
        public string? DoctorId { get; set; }
        public virtual Doctor? Doctor { get; set; }

        public virtual ICollection<Caregiver> Caregivers { get; set; } = new List<Caregiver>();
    }
}