using BreastCancer.DTO.request;
using BreastCancer.Models;

namespace BreastCancer.Repository.Interface
{
    public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
    {
        Task<RefreshToken?> CheckForExistingValidRefreshToken(ApplicationUser user);
        Task<RefreshToken?> GetByTokenAsync(string refreshToken);
    }
}
