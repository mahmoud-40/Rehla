using BreastCancer.Enum;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace BreastCancer.Models
{
    public class ApplicationUser : IdentityUser
    {

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        public string FullName => $"{FirstName} {LastName}";

        [MaxLength(500)]
        public string? Address { get; set; }

        public string? ImageUrl { get; set; }

        [DataType(DataType.Date)]
        [AllowNull]
        public DateTime? DateOfBirth { get; set; } = null;

        [NotMapped]
        public int? Age => DateOfBirth.HasValue ? CalculateAge() : null;

        [EnumDataType(typeof(Gender))]
        public Gender? Gender { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }


        public virtual Caregiver? Caregiver { get; set; } = null;
        public virtual Doctor? Doctor { get; set; } = null;
        public virtual Patient? Patient { get; set; } = null;
        public virtual ICollection<RefreshToken>? RefreshTokens { get; set; } = new List<RefreshToken>();
        private int CalculateAge()
        {
            if (!DateOfBirth.HasValue) return 0;
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Value.Year;
            if (DateOfBirth.Value.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}