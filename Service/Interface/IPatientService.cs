using BreastCancer.DTO.request;
using BreastCancer.DTO.response;

namespace BreastCancer.Service.Interface
{
    public interface IPatientService
    {
        Task<PatientResponseDTO?> GetPatientByIdAsync(string id);
        Task<IEnumerable<PatientResponseDTO>> GetAllPatientsAsync(int pageNumber = 1, int pageSize = 10);
        Task<PatientResponseDTO> CreatePatientAsync(PatientCreateDTO patientDto);
        Task<PatientResponseDTO?> UpdatePatientAsync(string id, PatientUpdateDTO patientDto);
        Task<bool> DeletePatientAsync(string id);
    }
}

