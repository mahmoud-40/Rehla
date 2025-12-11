using System.ComponentModel.DataAnnotations;

namespace BreastCancer.Models
{
    public class Patient
    {
        public string Id { get; set; }

        [MaxLength(2000)]
        public string MedicalHistory { get; set; } 

        public int UserId { get; set; }
        public virtual User? User { get; set; }

        public int? DoctorId {get;set;}
        public virtual Doctor? Doctor { get; set; }

        public virtual ICollection<Caregiver>? Caregivers { get; set; }
    }
}
