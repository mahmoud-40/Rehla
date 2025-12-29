using BreastCancer.DTO.request;
using BreastCancer.DTO.response;

namespace BreastCancer.Service.Interface
{
    public interface ICaregiverService
    {
        Task<IEnumerable<CaregiverResponse>> GetAllCaregivers();
        
        Task CreateCaregiver(CaregiverCreateDTO caregiverDto);

        Task<CaregiverResponse> GetCaregiverById(string id);

        Task UpdateCaregiver(string id, CaregiverUpdateDTO updateDto);

        Task DeleteCaregiver(string id);

        Task HardDeleteCaregiverById(string id);
        Task<IEnumerable<CaregiverResponse>> GetCaregiverByPatientId(string patientId);
    }
}
