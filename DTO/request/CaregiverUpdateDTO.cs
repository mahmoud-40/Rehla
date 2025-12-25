using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class CaregiverUpdateDTO
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address { get; set; }
        public string? ImageUrl { get; set; }
    }
}
