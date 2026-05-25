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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IUnitOfWork unitOfWork,
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _logger = logger;
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

            await _unitOfWork.NotificationsRepository.AddAsync(entity);
            await _unitOfWork.SaveAsync();

            var dto = MapToDto(entity);

            try
            {
                await _hubContext.Clients
                    .User(userId)
                    .SendAsync(NotificationHub.ReceiveNotificationMethod, dto);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to push notification {NotificationId} to user {UserId} via SignalR",
                    dto.Id,
                    userId);
            }

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
                await _unitOfWork.NotificationsRepository.GetByUserIdPagedAsync(userId, page, pageSize);

            return new PaginatedNotificationsResponse
            {
                Items = items.Select(MapToDto).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                UnreadCount = unreadCount
            };
        }

        public async Task<bool> MarkAsReadAsync(int id, string userId)
        {
            var updated = await _unitOfWork.NotificationsRepository.MarkAsReadAsync(id, userId);
            if (!updated)
            {
                return false;
            }

            await _unitOfWork.SaveAsync();
            return true;
        }

        public async Task<int> MarkAllAsReadAsync(string userId)
        {
            var count = await _unitOfWork.NotificationsRepository.MarkAllAsReadAsync(userId);
            if (count > 0)
            {
                await _unitOfWork.SaveAsync();
            }

            return count;
        }

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
