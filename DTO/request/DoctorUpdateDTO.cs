using BreastCancer.Enum;
using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class DoctorUpdateDTO
    {
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public string? ImageUrl { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [EnumDataType(typeof(Gender))]
        public Gender? Gender { get; set; }

        [MaxLength(100)]
        public string? Specialization { get; set; }

        [MaxLength(50)]
        public string? LicenseNumber { get; set; }

        [Range(0, 60)]
        public int? YearsOfExperience { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsVerified { get; set; }
    }
}

