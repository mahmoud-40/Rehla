using BreastCancer.Context;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Repository.Repositories
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(BreastCancerDB context) : base(context)
        {
        }

        public async Task AddAsync(Notification notification)
        {
            await base.AddAsync(notification);
        }

        public async Task<(IReadOnlyList<Notification> Items, int TotalCount, int UnreadCount)> GetByUserIdPagedAsync(
            string userId,
            int page,
            int pageSize)
        {
            var query = _Context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId);

            var totalCount = await query.CountAsync();
            var unreadCount = await query.CountAsync(n => !n.IsRead);

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount, unreadCount);
        }

        public async Task<Notification?> GetByIdForUserAsync(int id, string userId)
        {
            return await _Context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        }

        public async Task<bool> MarkAsReadAsync(int id, string userId)
        {
            var notification = await GetByIdForUserAsync(id, userId);
            if (notification is null)
            {
                return false;
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                Update(notification);
            }

            return true;
        }

        public async Task<int> MarkAllAsReadAsync(string userId)
        {
            var unread = await _Context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unread)
            {
                notification.IsRead = true;
                Update(notification);
            }

            return unread.Count;
        }
    }
}
