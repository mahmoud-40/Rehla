using BreastCancer.Enum;
using System.ComponentModel.DataAnnotations;

namespace BreastCancer.DTO.request
{
    public class CreateNotificationDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = null!;

        [Required]
        public NotificationType Type { get; set; }

        [MaxLength(100)]
        public string? TargetId { get; set; }
    }
}
