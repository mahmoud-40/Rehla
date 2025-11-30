using System.ComponentModel.DataAnnotations;

namespace BreastCancer.Models
{
    public class Doctor
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string Specialization { get; set; }

        [MaxLength(50)]
        public string License_Number { get; set; }


        [Range(0,60)]
        public int YearsOfExperience{ get; set; }
        public bool IsVerified{ get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }
        
        public ICollection<Patient>? Patients { get; set; }
    }
}
