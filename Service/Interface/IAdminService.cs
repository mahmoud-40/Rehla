using BreastCancer.DTO.request;
using BreastCancer.DTO.response;

namespace BreastCancer.Service.Interface
{
    public interface IAdminService
    {
        Task<IEnumerable<AdminUserResponseDTO>> GetAllUsersAsync();
        Task<IEnumerable<AdminUserResponseDTO>> GetUsersByRoleAsync(string role);
        Task DisableUserAsync(AdminDisableUserDTO request);
        Task DeleteUserAsync(AdminDeleteUserDTO request);
        Task AssignDoctorToPatientAsync(AssignDoctorToPatientDTO request);
        Task AssignCaregiverToPatientAsync(AssignCaregiverToPatientDTO request);
    }
}

