using System.ComponentModel.DataAnnotations;

namespace BreastCancer.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; }

        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public string Address { get; set; }
        public string ImageUrl { get; set; }

        public DateTime DateOfBirth { get; set; }
        //public int Age { get; set; }


        [RegularExpression("^(Male|Female)$")]
        public string Gender{ get; set; }

        [Phone]
        public string Phone{ get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Doctor? Doctor{ get; set; }
        public Patient? Patient{ get; set; }
        public Caregiver? Caregiver{ get; set; }


    }
}
