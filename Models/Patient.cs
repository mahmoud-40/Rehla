using System.ComponentModel.DataAnnotations;

namespace BreastCancer.Models
{
    public class Patient
    {
        public int Id { get; set; }

        [MaxLength(2000)]
        public string MedicalHistory { get; set; } 

        public int UserId { get; set; }
        public User? User { get; set; }

        public int? DoctorId {get;set;}
        public Doctor? Doctor { get; set; }

        public ICollection<Caregiver>? Caregivers { get; set; }
    }
}
