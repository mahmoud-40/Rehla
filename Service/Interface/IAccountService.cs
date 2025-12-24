using BreastCancer.DTO.request;
using BreastCancer.DTO.response;

namespace BreastCancer.Service.Interface { 
    public interface IAccountService
    {
        Task<TokenResponseDTO> LoginAsync(LoginDTO loginDTO);
        Task<(bool IsSuccess, IEnumerable<string> Errors)> DoctorRegister(DoctorRegisterDTO DoctorFromRequest);
        Task<(bool IsSuccess, IEnumerable<string> Errors)> CaregiverRegister(CaregiverRegisterDTO CaregiverFromRequest);
        Task<(bool IsSuccess, IEnumerable<string> Errors)> PatientRegister(PatientRegisterDTO PatientFromRequest);
        Task<bool> LogoutAsync(LogoutDTO dto);
        Task<TokenResponseDTO> RefreshTokenAsync(RefreshTokenDTO refreshToken);
    }
}
