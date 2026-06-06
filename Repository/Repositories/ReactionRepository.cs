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
    }
}
