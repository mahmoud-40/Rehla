using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace BreastCancer.Models
{
    public class Caregiver
    {
        public int Id { get; set; }

        public string RelationshipToPatient { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int PatientId { get; set; }
        public Patient? Patient { get; set; } 
    }
}
