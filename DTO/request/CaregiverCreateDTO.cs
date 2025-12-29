using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class CaregiverCreateDTO
    {
        [Required]
        public string FirstName { get; set; }
        [Required] 
        public string LastName { get; set; }

        [Required]
        public string Email { get; set; }
        [Required]
        public string PatientId { get; set; }
        public string? RelationshipType { get; set; }
        public string FullName => $"{FirstName} {LastName}";
    }
}
