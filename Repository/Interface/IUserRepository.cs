using BreastCancer.Models;

namespace BreastCancer.Repository.Interface
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
    }
}
