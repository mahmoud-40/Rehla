using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BreastCancer.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public const string HubRoute = "/hubs/notifications";
        public const string ReceiveNotificationMethod = "ReceiveNotification";
    }
}
