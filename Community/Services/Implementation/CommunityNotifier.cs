using BreastCancer.Community.Hubs;
using BreastCancer.Community.Services.Interface;
using Microsoft.AspNetCore.SignalR;

namespace BreastCancer.Community.Services.Implementation
{
    public class CommunityNotifier : ICommunityNotifier
    {
        private readonly IHubContext<CommunityHub> _hubContext;
        private readonly ILogger<CommunityNotifier> _logger;

        public CommunityNotifier(
            IHubContext<CommunityHub> hubContext,
            ILogger<CommunityNotifier> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyNewPostAsync(string userId, string postId)
        {
            try
            {
                await _hubContext.Clients
                    .Group(CommunityHub.UserGroup(userId))
                    .SendAsync(CommunityHub.NewPostAvailableMethod, postId);

                _logger.LogInformation(
                    "Pushed NewPostAvailable (postId={PostId}) to user {UserId}",
                    postId,
                    userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to push NewPostAvailable (postId={PostId}) to user {UserId}",
                    postId,
                    userId);
            }
        }
    }
}
