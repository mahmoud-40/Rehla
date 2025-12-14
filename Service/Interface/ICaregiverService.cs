using BreastCancer.DTO.request;

namespace BreastCancer.Service.Interface
{
    public interface ICaregiverService
    {
        Task<IEnumerable<CaregiverCreateDTO>> GetAllCaregiversAsync();
        void CreateCaregiver(CaregiverCreateDTO caregiverDto);
    }
}
