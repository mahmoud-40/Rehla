using BreastCancer.Models;

namespace BreastCancer.Repository.Interface
{
    public interface IUserRepository : IGenericRepository<User>
    {
        //Task<User?> GetByEmailAsync(string email);
    }
}
