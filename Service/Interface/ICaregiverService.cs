using BreastCancer.DTO.request;
using BreastCancer.Models;

namespace BreastCancer.Service.Interface
{
    public interface ICaregiverService
    {
        Task<IEnumerable<Caregiver>> GetAllCaregiversAsync();
        void CreateCaregiver(CaregiverCreateDTO caregiverDto);
    }
}
