using BreastCancer.Models;

namespace BreastCancer.Repository.Interface
{
    public interface INotificationRepository
    {
        Task<Notification> AddAsync(Notification notification);
        Task<(IReadOnlyList<Notification> Items, int TotalCount, int UnreadCount)> GetByUserIdPagedAsync(
            string userId,
            int page,
            int pageSize);
        Task<Notification?> GetByIdForUserAsync(int id, string userId);
        Task<bool> MarkAsReadAsync(int id, string userId);
        Task<int> MarkAllAsReadAsync(string userId);
    }
}
