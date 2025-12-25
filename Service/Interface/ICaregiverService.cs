using BreastCancer.DTO.request;
using BreastCancer.DTO.response;

namespace BreastCancer.Service.Interface
{
    public interface ICaregiverService
    {
        Task<IEnumerable<CaregiverResponse>> GetAllCaregivers();
        void CreateCaregiver(CaregiverCreateDTO caregiverDto);

        Task<CaregiverResponse> GetCaregiverById(string id);

        Task UpdateCaregiver(string id, CaregiverUpdateDTO updateDto);

        void DeleteCaregiver(string id);

        void HardDeleteCaregiverById(string id);
        Task<IEnumerable<CaregiverResponse>> GetCaregiverByPatientId(string patientId);
    }
}
