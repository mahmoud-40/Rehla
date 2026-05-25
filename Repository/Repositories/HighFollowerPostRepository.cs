using BreastCancer.Context;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;

namespace BreastCancer.Repository.Repositories;

public class HighFollowerPostRepository : GenericRepository<HighFollowerPost>, IHighFollowerPostRepository
{
    public HighFollowerPostRepository(BreastCancerDB _Context) : base(_Context)
    {
    }
}
