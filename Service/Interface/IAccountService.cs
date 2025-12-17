using BreastCancer.DTO.request;

namespace BreastCancer.Service.Interface { 
    public interface IAccountService
    {
        Task<(bool IsSuccess, IEnumerable<string> Errors)> RegisterAsync(RegisterDTO registerDTO);
        Task<(bool IsSuccess, string Token, string ErrorsMessage)> LoginAsync(LoginDTO loginDTO);

    }
}
