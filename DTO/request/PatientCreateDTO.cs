using BreastCancer.Enum;
using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class PatientCreateDTO
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public string? ImageUrl { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [EnumDataType(typeof(Gender))]
        public Gender? Gender { get; set; }

        [MaxLength(2000)]
        public string? MedicalHistory { get; set; }

        public string? DoctorId { get; set; }
    }
}

