using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BreastCancer.DTO.request
{
    public class DoctorRegisterDTO : BaseRegisterDTO
    {
        [MaxLength(100)]
        public string? Specialization { get; set; }

        [MaxLength(50)]
        public string? LicenseNumber { get; set; }

        [Range(0, 60)]
        public int? YearsOfExperience { get; set; }

        [Required]
        public string? NationalIdImagePath { get; set; }
        public override string Role => "Doctor";

        [JsonIgnore]
        public bool IsVerified { get; set; } = false;
    }
}
