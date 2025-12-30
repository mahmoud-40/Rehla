using BreastCancer.DTO.request;
using BreastCancer.DTO.response;

namespace BreastCancer.Service.Interface
{
    public interface IDoctorService
    {
        Task<DoctorResponseDTO?> GetDoctorByIdAsync(string id);
        Task<IEnumerable<DoctorResponseDTO>> GetAllDoctorsAsync(int pageNumber = 1, int pageSize = 10);
        Task<DoctorResponseDTO> CreateDoctorAsync(DoctorCreateDTO doctorDto);
        Task<DoctorResponseDTO?> UpdateDoctorAsync(string id, DoctorUpdateDTO doctorDto);
        Task DeleteDoctorAsync(string id);
        Task HardDeleteDoctorAsync(string id);
    }
}

