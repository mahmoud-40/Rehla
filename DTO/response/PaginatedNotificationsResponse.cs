namespace BreastCancer.DTO.response
{
    public class PaginatedNotificationsResponse
    {
        public IReadOnlyList<NotificationDto> Items { get; set; } = Array.Empty<NotificationDto>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
    }
}
