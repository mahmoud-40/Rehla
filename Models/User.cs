using System.ComponentModel.DataAnnotations;

namespace BreastCancer.Models
{
    public class User 
    {
        public string Id { get; set; }
        public string FullName { get; set; }

        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        public string Address { get; set; }
        public string ImageUrl { get; set; }

        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }


        [RegularExpression("^(Male|Female)$")]
        public string Gender{ get; set; }

        [Phone]
        public string Phone{ get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

        public virtual string CreatedBy { get; set; }
        public virtual string UpdatedBy { get; set; }

        public virtual Doctor? Doctor{ get; set; }
        public virtual Patient? Patient{ get; set; }
        public virtual Caregiver? Caregiver{ get; set; }


    }
}
