using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Templates;
using Microsoft.AspNetCore.Identity;

namespace BreastCancer.Service.Interface { 
    public interface IAccountService
    {
        Task<TokenResponseDTO> LoginAsync(LoginDTO loginDTO);
        Task<(bool IsSuccess, IEnumerable<string> Errors)> DoctorRegister(DoctorRegisterDTO DoctorFromRequest);
        Task<(bool IsSuccess, IEnumerable<string> Errors)> CaregiverRegister(CaregiverRegisterDTO CaregiverFromRequest);
        Task<(bool IsSuccess, IEnumerable<string> Errors)> PatientRegister(PatientRegisterDTO PatientFromRequest);
        Task<bool> LogoutAsync(LogoutDTO dto);
        Task<TokenResponseDTO> RefreshTokenAsync(RefreshTokenDTO refreshToken);

        Task<(bool IsSuccess, IEnumerable<string> Errors)> ConfirmEmailAsync(ConfirmEmailDTO Confirm);

        Task<(bool IsSuccess, IEnumerable<string> Errors)> ResendConfirmationCodeAsync(string Email);
        Task<(bool IsSuccess, IEnumerable<string> Errors)> ResetPasswordAsync(ResetPasswordDTO resetPassword);
        Task<(bool IsSuccess, IEnumerable<string> Errors)> SendForgetPasswordCodeAsync(string Email);
        Task<(bool IsSuccess, IEnumerable<string> Errors)> ForgetPasswordAsync(ForgetPasswordDTO forgetPassword);
    }
}
