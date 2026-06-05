namespace BreastCancer.Community.Services.Interface
{
    public interface ICommunityNotifier
    {
        Task NotifyNewPostAsync(string userId, string postId);
    }
}
