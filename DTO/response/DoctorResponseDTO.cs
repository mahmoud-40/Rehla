using BreastCancer.Enum;

namespace BreastCancer.DTO.response
{
    public class DoctorResponseDTO
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Age { get; set; }
        public Gender? Gender { get; set; }
        public bool IsActive { get; set; }
        public string? Specialization { get; set; }
        public string? LicenseNumber { get; set; }
        public int? YearsOfExperience { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

