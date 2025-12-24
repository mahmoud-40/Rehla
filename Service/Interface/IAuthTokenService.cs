using BreastCancer.Models;

namespace BreastCancer.Service.Interface
{
    public interface IAuthTokenService
    {
        Task<(string Token, DateTime expiresAtUtc)> GenerateToken(ApplicationUser user);
        Task<string> GenerateRefreshToken();
    }
}