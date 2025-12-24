using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Models;
using BreastCancer.Options;
using BreastCancer.Repository.Interface;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BreastCancer.Service.Implementation
{
    public class AuthTokenService : IAuthTokenService
    {
        private readonly JwtOptions _jwtOptions; 
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;

        public AuthTokenService(
            IOptions<JwtOptions> jwtOptions, 
            UserManager<ApplicationUser> userManager, 
            RoleManager<ApplicationRole> roleManager,
            IUnitOfWork unitOfWork    
        )
        {
            this._jwtOptions = jwtOptions.Value;
            this._roleManager = roleManager;
            this._unitOfWork = unitOfWork;
            this._userManager = userManager;
        }
        public async Task<(string Token,DateTime expiresAtUtc)> GenerateToken(ApplicationUser user)
        {
            // Claims
            List<Claim> UserClaims = new List<Claim>()
            {
                // JTI
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                // addition Info
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email,user.Email)
            };

            // Add Roles
            IEnumerable<string> UserRoles = await _userManager.GetRolesAsync(user);
            foreach (var Role in UserRoles)
            {
                UserClaims.Add(new Claim(ClaimTypes.Role, Role));
            }

            // Signing Credentials
            var SignKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
            SigningCredentials signingCredentials = new SigningCredentials(SignKey, SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationTimeInMinutes);

            JwtSecurityToken myToken = new JwtSecurityToken(
                    issuer: _jwtOptions.Issuer,
                    audience: _jwtOptions.Audience,
                    expires: expires.ToLocalTime(),
                    claims: UserClaims,
                    signingCredentials: signingCredentials
                );
            var tokenHandler = new JwtSecurityTokenHandler().WriteToken(myToken);
            return (tokenHandler, expires);
        }
        public async Task<string> GenerateRefreshToken()
        {
            var randomNumer = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumer);
            return Convert.ToBase64String(randomNumer);
        }

        
    }
}
