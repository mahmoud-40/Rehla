using BreastCancer.Context;
using BreastCancer.DTO.request;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Repository.Repositories
{
    public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(BreastCancerDB _Context) : base(_Context)
        {
        }

        public async Task<RefreshToken?> GetByTokenAsync(string refreshToken)
        {
            return await _dbSet.FirstOrDefaultAsync(t => t.Token == refreshToken);
        }
        public async Task<RefreshToken?> CheckForExistingValidRefreshToken(ApplicationUser user)
        {
            DateTime CurrentTime = DateTime.UtcNow.ToLocalTime();
            return await _dbSet
                .Where(t => t.UserId == user.Id && !t.IsRevoked && t.ExpiresAt > CurrentTime)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
