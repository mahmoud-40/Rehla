using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace BreastCancer.Models
{
    public class Caregiver
    {
        public string Id { get; set; }

        public string RelationshipType { get; set; }

        public string UserId { get; set; }
        public virtual User? User { get; set; }

        public string PatientId { get; set; }
        public bool IsDeleted { get; set; } = false;
        public virtual Patient? Patient { get; set; } 
    }
}
