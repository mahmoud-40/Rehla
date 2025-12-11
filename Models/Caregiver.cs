using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace BreastCancer.Models
{
    public class Caregiver
    {
        public string Id { get; set; }

        public string RelationshipType { get; set; }

        public int UserId { get; set; }
        public virtual User? User { get; set; }

        public int PatientId { get; set; }
        public virtual Patient? Patient { get; set; } 
    }
}
