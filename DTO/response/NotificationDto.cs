using BreastCancer.Enum;

namespace BreastCancer.DTO.response
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public NotificationType Type { get; set; }
        public string? TargetId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
