using BreastCancer.DTO.response;
using BreastCancer.Hubs;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.SignalR;

namespace BreastCancer.Service.Implementation
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(
            INotificationRepository notificationRepository,
            IHubContext<NotificationHub> hubContext)
        {
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
        }

        public async Task<NotificationDto> SendNotificationAsync(string userId, NotificationDto payload)
        {
            var entity = new Notification
            {
                UserId = userId,
                Title = payload.Title,
                Message = payload.Message,
                Type = payload.Type,
                TargetId = payload.TargetId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            var saved = await _notificationRepository.AddAsync(entity);
            var dto = MapToDto(saved);

            await _hubContext.Clients
                .User(userId)
                .SendAsync(NotificationHub.ReceiveNotificationMethod, dto);

            return dto;
        }

        public async Task<PaginatedNotificationsResponse> GetUserNotificationsAsync(
            string userId,
            int page,
            int pageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

            var (items, totalCount, unreadCount) =
                await _notificationRepository.GetByUserIdPagedAsync(userId, page, pageSize);

            return new PaginatedNotificationsResponse
            {
                Items = items.Select(MapToDto).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                UnreadCount = unreadCount
            };
        }

        public Task<bool> MarkAsReadAsync(int id, string userId)
            => _notificationRepository.MarkAsReadAsync(id, userId);

        public Task<int> MarkAllAsReadAsync(string userId)
            => _notificationRepository.MarkAllAsReadAsync(userId);

        private static NotificationDto MapToDto(Notification notification) => new()
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            TargetId = notification.TargetId,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
    }
}
