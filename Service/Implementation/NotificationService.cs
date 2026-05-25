using AutoMapper;
using BreastCancer.DTO.request;
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
        private readonly IMapper _mapper;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<NotificationDto> SendNotificationAsync(string userId, CreateNotificationDto payload)
        {
            var entity = _mapper.Map<Notification>(payload);
            entity.UserId = userId;
            entity.IsRead = false;
            entity.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.NotificationRepository.AddAsync(entity);
            await _unitOfWork.SaveAsync();

            var dto = _mapper.Map<NotificationDto>(entity);

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

            var all = (await _unitOfWork.NotificationRepository.FilterAsync(
                n => n.UserId == userId,
                q => q.OrderByDescending(n => n.CreatedAt))).ToList();

            var items = all
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PaginatedNotificationsResponse
            {
                Items = _mapper.Map<IReadOnlyList<NotificationDto>>(items),
                Page = page,
                PageSize = pageSize,
                TotalCount = all.Count,
                UnreadCount = all.Count(n => !n.IsRead)
            };
        }

        public async Task<bool> MarkAsReadAsync(int id, string userId)
        {
            var matches = await _unitOfWork.NotificationRepository.FilterAsync(
                n => n.Id == id && n.UserId == userId);

            var notification = matches.FirstOrDefault();
            if (notification is null)
            {
                return false;
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                _unitOfWork.NotificationRepository.Update(notification);
                await _unitOfWork.SaveAsync();
            }

            return true;
        }

        public async Task<int> MarkAllAsReadAsync(string userId)
        {
            var unread = (await _unitOfWork.NotificationRepository.FilterAsync(
                n => n.UserId == userId && !n.IsRead)).ToList();

            foreach (var notification in unread)
            {
                notification.IsRead = true;
                _unitOfWork.NotificationRepository.Update(notification);
            }

            if (unread.Count > 0)
            {
                await _unitOfWork.SaveAsync();
            }

            return unread.Count;
        }
    }
}
