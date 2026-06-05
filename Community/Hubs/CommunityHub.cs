using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace BreastCancer.Community.Hubs
{
    [Authorize]
    public class CommunityHub : Hub
    {
        public const string HubRoute = "/hubs/community";
        public const string NewPostAvailableMethod = "NewPostAvailable";

        public static string UserGroup(string userId) => $"user:{userId}";

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId is not null)
                await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
            await base.OnConnectedAsync();
        }
    }
}
