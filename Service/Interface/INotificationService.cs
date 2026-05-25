using BreastCancer.DTO.response;

namespace BreastCancer.Service.Interface
{
    public interface INotificationService
    {
        Task<NotificationDto> SendNotificationAsync(string userId, NotificationDto payload);
        Task<PaginatedNotificationsResponse> GetUserNotificationsAsync(string userId, int page, int pageSize);
        Task<bool> MarkAsReadAsync(int id, string userId);
        Task<int> MarkAllAsReadAsync(string userId);
    }
}
