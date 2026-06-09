using BreastCancer.Context;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;

namespace BreastCancer.Repository.Repositories
{
    public class ReactionRepository : GenericRepository<Reaction>, IReactionRepository
    {
        public ReactionRepository(BreastCancerDB _Context) : base(_Context)
        {
        }

        public Task<Reaction?> GetReactionByPostIdAndUserIdAsync(int postId, string userId)
        {
            var reaction = _Context.Reactions.FirstOrDefault(r => r.PostId == postId && r.UserId == userId);
            return reaction != null ? Task.FromResult<Reaction?>(reaction) : Task.FromResult<Reaction?>(null);
        }
    }
}
