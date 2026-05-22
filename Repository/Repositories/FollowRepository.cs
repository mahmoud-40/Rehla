using BreastCancer.Context;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;

namespace BreastCancer.Repository.Repositories
{
    public class FollowRepository : GenericRepository<Follow>, IFollowRepository
    {
        public FollowRepository(BreastCancerDB _Context) : base(_Context)
        {
        }
    }
}
