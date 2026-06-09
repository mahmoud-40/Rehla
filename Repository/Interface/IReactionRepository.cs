using BreastCancer.Models;

namespace BreastCancer.Repository.Interface
{
    public interface IReactionRepository : IGenericRepository<Reaction>
    {
        Task<Reaction?> GetReactionByPostIdAndUserIdAsync(int postId, string userId);
    }
}
