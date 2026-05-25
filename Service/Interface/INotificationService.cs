using BreastCancer.DTO.response;

namespace BreastCancer.Service.Interface
{
    public class CreateNotificationDto
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public int? TargetId { get; set; }
    }

    public interface INotificationService
    {
        Task<NotificationDto> SendNotificationAsync(string userId, CreateNotificationDto payload);
        Task<PaginatedNotificationsResponse> GetUserNotificationsAsync(string userId, int page, int pageSize);
        Task<bool> MarkAsReadAsync(int id, string userId);
        Task<int> MarkAllAsReadAsync(string userId);
    }
}
