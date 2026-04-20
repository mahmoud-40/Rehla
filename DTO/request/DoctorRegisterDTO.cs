using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BreastCancer.DTO.request
{
    public class DoctorRegisterDTO : BaseRegisterDTO
    {
        [Required(ErrorMessage = "Specialization is required.")]
        [MaxLength(100, ErrorMessage = "Specialization cannot exceed 100 characters.")]
        public string Specialization { get; set; }

        [Required(ErrorMessage = "License number is required.")]
        [MaxLength(50, ErrorMessage = "License number cannot exceed 50 characters.")]
        public string LicenseNumber { get; set; }

        [Required(ErrorMessage = "Years of experience is required.")]
        [Range(0, 60, ErrorMessage = "Years of experience must be between 0 and 60.")]
        public int YearsOfExperience { get; set; }

        [Required(ErrorMessage = "National ID image is required.")]
        public string NationalIdImagePath { get; set; }

        public override string Role => "Doctor";

        [JsonIgnore]
        public bool IsVerified { get; set; } = false;
    }
}