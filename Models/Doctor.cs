using System.ComponentModel.DataAnnotations;

namespace BreastCancer.Models
{
    public class Doctor 
    {
        public string Id { get; set; }

        [MaxLength(100)]
        public string Specialization { get; set; }

        [MaxLength(50)]
        public string License_Number { get; set; }


        [Range(0,60)]
        public int YearsOfExperience{ get; set; }
        public bool IsVerified { get; set; } = false;

        public int UserId { get; set; }

        public virtual User? User { get; set; }
        
        public virtual ICollection<Patient>? Patients { get; set; }
    }
}
