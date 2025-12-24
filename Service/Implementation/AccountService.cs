using AutoMapper;
using BreastCancer.Context;
using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Models;
using BreastCancer.Options;
using BreastCancer.Repository.Interface;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BreastCancer.Service.Implementation
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly JwtOptions _jwtOptions;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOptions<JwtOptions> jwtOptions;
        private readonly IAuthTokenService _authToken;
        private readonly IMapper _mapper;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IUnitOfWork unitOfWork,
            IOptions<JwtOptions> jwtOptions,
            IAuthTokenService authToken,
            IMapper mapper
            )
        {
            this._userManager = userManager;
            this._roleManager = roleManager;
            this._unitOfWork = unitOfWork;
            this.jwtOptions = jwtOptions;
            this._authToken = authToken;
            this._mapper = mapper;
            this._jwtOptions = jwtOptions.Value;
        }
        
        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> DoctorRegister(DoctorRegisterDTO DoctorFromRequest)
        {

            var userResult = await CreateUserAsync(DoctorFromRequest);

            if (!userResult.IsSuccess)
            {
                return (false, userResult.Errors);
            }


            var doctor = new Doctor
            {
                UserId = userResult.Id,
                LicenseNumber = DoctorFromRequest.LicenseNumber,
                Specialization = DoctorFromRequest.Specialization,
                YearsOfExperience = DoctorFromRequest.YearsOfExperience
            };

            _unitOfWork.DoctorsRepository.Add(doctor);
            await _unitOfWork.SaveAsync();

            return (true, null);
        }

        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> CaregiverRegister(CaregiverRegisterDTO CaregiverFromRequest)
        {
            var userResult = await CreateUserAsync(CaregiverFromRequest);

            if (!userResult.IsSuccess)
            {
                return (false, userResult.Errors);
            }


            var caregiver = new Caregiver
            {
                UserId = userResult.Id,
                RelationshipType = CaregiverFromRequest.RelationshipType,
                PatientId = CaregiverFromRequest.PatientId

            };

            _unitOfWork.CaregiversRepository.Add(caregiver);
            await _unitOfWork.SaveAsync();

            return (true, null);

        }

        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> PatientRegister(PatientRegisterDTO PatientFromRequest)
        {
            var userResult = await CreateUserAsync(PatientFromRequest);

            if (!userResult.IsSuccess)
            {
                return (false, userResult.Errors);
            }

            var patient = new Patient
            {
                UserId = userResult.Id,
                MedicalHistory = PatientFromRequest.MedicalHistory,
                DoctorId = null // will be assigned later

            };

            _unitOfWork.PatientsRepository.Add(patient);
            await _unitOfWork.SaveAsync();

            return (true, null);

        }
        public async Task<TokenResponseDTO> LoginAsync(LoginDTO loginDTO)
        {
            var user = await _userManager.FindByEmailAsync(loginDTO.Email);

            if (user == null)
                return new TokenResponseDTO
                {
                    IsSuccess =false,
                    Errors = new[] {"Invaild Email or Password"}
                };

            bool IsValid = await _userManager.CheckPasswordAsync(user, loginDTO.Password);

            if (!IsValid) 
                return new TokenResponseDTO
                {
                    IsSuccess = false,
                    Errors = new[] { "Invaild Email or Password" }
                };


            var AccessToken = await _authToken.GenerateToken(user);

            var existingToken = await _unitOfWork.RefreshTokenRepository.CheckForExistingValidRefreshToken(user);

            string refreshToken;
            if (existingToken != null)
            {
                // Reuse the existing valid refresh token
                refreshToken = existingToken.Token;
            }
            else
            {
                // Create a new refresh token
                var newRefreshToken = new RefreshToken
                {
                    Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                    UserId = user.Id,
                    ExpiresAt = DateTime.UtcNow.AddDays(7).ToLocalTime(),
                    IsRevoked = false
                };

                _unitOfWork.RefreshTokenRepository.Add(newRefreshToken);
                await _unitOfWork.SaveAsync();

                refreshToken = newRefreshToken.Token;
            }
            

            return new TokenResponseDTO
            {
               IsSuccess = true,
               RefreshToken= refreshToken,
               AccessToken = AccessToken.Token,
               ExpiresTime = AccessToken.expiresAtUtc
            };

        }

        private async Task<(string Id,bool IsSuccess, IEnumerable<string> Errors)>  CreateUserAsync (BaseRegisterDTO model)
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
                return (null,false, new[] { "Email Already Exists" });


            // ToDo: AutoMapper
            var user = new ApplicationUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.Email,
                Email = model.Email,
                Address = model.Address,
                PhoneNumber = model.PhoneNumber,
                Gender = model.Gender,
                DateOfBirth = model.DateOfBirth
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return (null,false, result.Errors.Select(e => e.Description));

            // Assign Role
            var roleResult = await _userManager.AddToRoleAsync(user, model.Role);

            if (!roleResult.Succeeded)
                return (null,false, roleResult.Errors.Select(e => e.Description));

            return (user.Id, true, null);
        }

        public async Task<bool> LogoutAsync(LogoutDTO dto)
        {
            var token = await _unitOfWork.RefreshTokenRepository.GetByTokenAsync(dto.RefreshToken);

            if (token == null)
                return false;

            if (token.IsRevoked)
                return true; // already logged out

            token.IsRevoked = true;
            _unitOfWork.RefreshTokenRepository.Update(token);
            await _unitOfWork.SaveAsync();

            return true;
        }
        public async Task<TokenResponseDTO> RefreshTokenAsync(RefreshTokenDTO Token)
        {
            var tokenEntity = await _unitOfWork.RefreshTokenRepository.GetByTokenAsync(Token.RefreshToken);

            if (tokenEntity == null || tokenEntity.ExpiresAt < DateTime.UtcNow || tokenEntity.IsRevoked)
            {
                return new TokenResponseDTO
                {
                    IsSuccess = false,
                    Errors = new[] { "Invalid or expired refresh token" }
                };
            }
            var user = tokenEntity.User;

            var accessToken = await _authToken.GenerateToken(user);

            return new TokenResponseDTO
            {
                IsSuccess = true,
                AccessToken = accessToken.Token,
                ExpiresTime = accessToken.expiresAtUtc.ToLocalTime(),
                RefreshToken = Token.RefreshToken
            };
        }
    }
}