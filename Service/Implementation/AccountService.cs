using AutoMapper;
using BreastCancer.Context;
using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Models;
using BreastCancer.Options;
using BreastCancer.Repository.Interface;
using BreastCancer.Service.Interface;
using BreastCancer.Templates;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
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
        private readonly IEmailService _emailService;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IUnitOfWork unitOfWork,
            IOptions<JwtOptions> jwtOptions,
            IAuthTokenService authToken,
            IMapper mapper,
            IEmailService emailService
            )
        {
            this._userManager = userManager;
            this._roleManager = roleManager;
            this._unitOfWork = unitOfWork;
            this.jwtOptions = jwtOptions;
            this._authToken = authToken;
            this._mapper = mapper;
            this._jwtOptions = jwtOptions.Value;
            this._emailService = emailService;
        }

        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> DoctorRegister(DoctorRegisterDTO DoctorFromRequest)
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var userResult = await CreateUserAsync(DoctorFromRequest);
                if (!userResult.IsSuccess)
                    return (false, userResult.Errors);

                var doctor = new Doctor
                {
                    UserId = userResult.Id,
                    LicenseNumber = DoctorFromRequest.LicenseNumber,
                    Specialization = DoctorFromRequest.Specialization,
                    YearsOfExperience = DoctorFromRequest.YearsOfExperience,
                    NationalIdImage = DoctorFromRequest.NationalIdImagePath
                };

                _unitOfWork.DoctorsRepository.Add(doctor);
                await _unitOfWork.SaveAsync();
                await transaction.CommitAsync();

                await _emailService.SendEmailAsync(userResult.EmailTo!, "Confirm Your Email Address", userResult.EmailBody!);

                return (true, null);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> CaregiverRegister(CaregiverRegisterDTO CaregiverFromRequest)
        {
            var patient = await _unitOfWork.PatientsRepository.GetByEmailAsync(CaregiverFromRequest.PatientEmail);
            if (patient == null)
                return (false, new[] { "Invalid Email Patient" });

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var userResult = await CreateUserAsync(CaregiverFromRequest);
                if (!userResult.IsSuccess)
                    return (false, userResult.Errors);

                var caregiver = new Caregiver
                {
                    UserId = userResult.Id,
                    RelationshipType = CaregiverFromRequest.RelationshipType,
                    PatientId = patient.UserId
                };

                _unitOfWork.CaregiversRepository.Add(caregiver);
                await _unitOfWork.SaveAsync();
                await transaction.CommitAsync();

                await _emailService.SendEmailAsync(userResult.EmailTo!, "Confirm Your Email Address", userResult.EmailBody!);

                return (true, null);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> PatientRegister(PatientRegisterDTO PatientFromRequest)
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var userResult = await CreateUserAsync(PatientFromRequest);
                if (!userResult.IsSuccess)
                    return (false, userResult.Errors);

                var patient = new Patient
                {
                    UserId = userResult.Id,
                    MedicalHistory = PatientFromRequest.MedicalHistory,
                    DoctorId = null
                };

                _unitOfWork.PatientsRepository.Add(patient);
                await _unitOfWork.SaveAsync();
                await transaction.CommitAsync();

                await _emailService.SendEmailAsync(userResult.EmailTo!, "Confirm Your Email Address", userResult.EmailBody!);

                return (true, null);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<(string Id, bool IsSuccess, IEnumerable<string> Errors, string? EmailTo, string? EmailBody)> CreateUserAsync(BaseRegisterDTO model)
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
                return (null!, false, new[] { "An account with this email already exists." }, null, null);

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
                return (null!, false, result.Errors.Select(e => e.Description), null, null);

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            user.EmailConfirmationCode = code;
            user.EmailConfirmationCodeExpireAt = DateTime.UtcNow.AddMinutes(5).ToLocalTime();
            await _userManager.UpdateAsync(user);

            var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
            if (!roleResult.Succeeded)
                return (null!, false, roleResult.Errors.Select(e => e.Description), null, null);

            var emailBody = EmailTemplates.GetConfirmationEmail(user.FullName, user.EmailConfirmationCode);

            return (user.Id!, true, null!, user.Email!, emailBody);
        }

        public async Task<TokenResponseDTO> LoginAsync(LoginDTO loginDTO)
        {
            var user = await _userManager.FindByEmailAsync(loginDTO.Email);

            if (user == null)
                return new TokenResponseDTO
                {
                    IsSuccess = false,
                    Errors = new[] { "Invalid email or password." }
                };

            var checkIfTheEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);

            if (!checkIfTheEmailConfirmed)
                return new TokenResponseDTO
                {
                    IsSuccess = false,
                    Errors = new[] { "Please confirm your email first" }
                };


            bool IsValid = await _userManager.CheckPasswordAsync(user, loginDTO.Password);

            if (!IsValid)
                return new TokenResponseDTO
                {
                    IsSuccess = false,
                    Errors = new[] { "Invalid email or password." }
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
                RefreshToken = refreshToken,
                AccessToken = AccessToken.Token,
                ExpiresTime = AccessToken.expiresAtUtc
            };

        }

        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> ConfirmEmailAsync(ConfirmEmailDTO Confirm)
        {
            var user = await _userManager.FindByEmailAsync(Confirm.Email);
            if (user == null)
                return (false, new[] { "Invalid user" });

            if (user.EmailConfirmed)
                return (false, new[] { "This email has already been confirmed." });

            if (user.EmailConfirmationCodeExpireAt < DateTime.UtcNow.ToLocalTime())
                return (false, new[] { "The confirmation code has expired." });

            if (user.EmailConfirmationCode != Confirm.Code)
                return (false, new[] { "Invalid confirmation code." });

            var result = await _userManager.ConfirmEmailAsync(user, Confirm.Code);

            if (!result.Succeeded)
                return (false, new[] { "Invalid confirmation code." });

            user.EmailConfirmationCodeExpireAt = null;

            await _userManager.UpdateAsync(user);

            return (true, null!);

        }

        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> ResendConfirmationCodeAsync(string Email)
        {
            var user = await _userManager.FindByEmailAsync(Email);

            if (user == null)
                return (false, new[] { "Invaild user" });

            if (user.EmailConfirmed)
                return (false, new[] { "This email has already been confirmed." });

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            user.EmailConfirmationCode = code;
            user.EmailConfirmationCodeExpireAt = DateTime.UtcNow.AddMinutes(5).ToLocalTime();

            await _userManager.UpdateAsync(user);

            var body = EmailTemplates.GetConfirmationEmail(user.FullName, user.EmailConfirmationCode);
            await _emailService.SendEmailAsync(user.Email!, "Confirm Your Email Address", body);

            return (true, null!);
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

            if (tokenEntity == null || tokenEntity.ExpiresAt < DateTime.UtcNow.ToLocalTime() || tokenEntity.IsRevoked)
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

        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> ResetPasswordAsync(ResetPasswordDTO resetPassword)
        {
            var user = await _userManager.FindByEmailAsync(resetPassword.Email);
            if (user == null)
                return (false, new[] { "Invalid user" });

            // Validate current password
            if (!await _userManager.CheckPasswordAsync(user, resetPassword.CurrentPassword))
                return (false, new[] { "Current password is incorrect." });

            // Prevent reuse
            if (await _userManager.CheckPasswordAsync(user, resetPassword.Password))
                return (false, new[] { "New password must be different from the old password." });

            var result = await _userManager.ChangePasswordAsync(user, resetPassword.CurrentPassword, resetPassword.Password);
            await _userManager.UpdateSecurityStampAsync(user);
            if (result.Succeeded)
            {
                var body = EmailTemplates.GetPasswordResetSuccessEmail(user.FullName);
                await _emailService.SendEmailAsync(user.Email!, "Your Password Has Been Changed", body);
                return (true, null!);
            }

            var errors = result.Errors
                    .Select(e => e.Description)
                    .ToArray();

            return (false, errors);

        }

        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> SendForgetPasswordCodeAsync(string Email)
        {
            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
                return (false, new[] { "Invalid user" });

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var numericCode = GenerateNumericCode();

            user.PasswordResetToken = resetToken;
            user.PasswordResetCode = numericCode;
            user.PasswordResetExpireAt = DateTime.UtcNow.AddMinutes(5).ToLocalTime();

            var result = await _userManager.UpdateAsync(user);


            if (result.Succeeded)
            {
                var body = EmailTemplates.GetForgetPasswordEmail(user.FullName, numericCode);
                await _emailService.SendEmailAsync(user.Email!, "Reset Your Password", body);
                return (true, null!);
            }

            var errors = result.Errors
                    .Select(e => e.Description)
                    .ToArray();

            return (false, errors);

        }
        public async Task<(bool IsSuccess, IEnumerable<string> Errors)> ForgetPasswordAsync(ForgetPasswordDTO forgetPassword)
        {
            var user = await _userManager.FindByEmailAsync(forgetPassword.Email);
            if (user == null)
                return (false, new[] { "Invalid user" });

            if (user.PasswordResetExpireAt == null || user.PasswordResetExpireAt < DateTime.UtcNow.ToLocalTime())
                return (false, new[] { "The confirmation code has expired." });

            if (user.PasswordResetCode != forgetPassword.Code)
                return (false, new[] { "Invalid reset code." });

            var result = await _userManager.ResetPasswordAsync(user, user.PasswordResetToken!, forgetPassword.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .Select(e => e.Description)
                    .ToArray();

                return (false, errors);
            }


            user.PasswordResetExpireAt = null;
            user.PasswordResetToken = null;
            user.PasswordResetCode = null;

            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);

            var body = EmailTemplates.GetPasswordResetSuccessEmail(user.FullName);
            await _emailService.SendEmailAsync(user.Email!, "Your Password Has Been Changed", body);

            return (true, null!);

        }

        private static string GenerateNumericCode(int length = 6)
        {
            var bytes = RandomNumberGenerator.GetBytes(length);
            var result = new StringBuilder(length);

            foreach (var b in bytes)
                result.Append((b % 10).ToString());

            return result.ToString();
        }
    
        
    }
}