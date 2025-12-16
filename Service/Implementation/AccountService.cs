using BreastCancer.DTO.request;
using BreastCancer.Models;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BreastCancer.Service.Implementation
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration configuration;

        public AccountService(UserManager<ApplicationUser> userManager ,IConfiguration configuration)
        {
            this.userManager = userManager;
            this.configuration = configuration;
        }
        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> RegisterAsync(RegisterDTO registerDTO)
        {
            var user = new ApplicationUser
            {
                FirstName = registerDTO.FirstName,
                LastName = registerDTO.LastName,
                UserName = registerDTO.Username,
                PhoneNumber = registerDTO.PhoneNumber,
                Email = registerDTO.Email
            };

            var result = await userManager.CreateAsync(user, registerDTO.Password);

            if (result.Succeeded)
                return (true, null);

            return (false, result.Errors.Select(e => e.Description));
        }

        public async Task<(bool IsSuccess, string Token, string ErrorsMessage)> LoginAsync(LoginDTO loginDTO)
        {
            var user = await userManager.FindByEmailAsync(loginDTO.Email);

            if (user == null)
                return (false, null, "InValid Email or Password");

            bool IsValid = await userManager.CheckPasswordAsync(user, loginDTO.Password);
            
            if( !IsValid)
                return (false, null, "InValid Email or Password");

            // Generate Token
            // Claims
            List<Claim> UserClaims = new List<Claim>();
            // JTI
            UserClaims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            // addition Info
            UserClaims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            UserClaims.Add(new Claim(ClaimTypes.Name, user.FullName));
            // Add Roles
            IEnumerable<string> UserRoles = await userManager.GetRolesAsync(user);
            foreach (var Role in UserRoles)
            {
                UserClaims.Add(new Claim(ClaimTypes.Role, Role));
            }

            // Signing Credentials
            var SignKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Key"]));
            SigningCredentials signingCredentials = new SigningCredentials(SignKey, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken myToken = new JwtSecurityToken(
                    issuer: "https://localhost:44305/",
                    expires: DateTime.Now.AddHours(2),
                    claims : UserClaims,
                    signingCredentials: signingCredentials
                );

            var tokenHandler = new JwtSecurityTokenHandler().WriteToken(myToken);

            return (true, tokenHandler, null);


        }

    }
}
