using BreastCancer.DTO.request;
using BreastCancer.Repository.Interface;
using BreastCancer.Service.Interface;

namespace BreastCancer.Service.Implementation
{
    public class CaregiverService : ICaregiverService
    {
        private readonly IUnitOfWork _unitOfWork;
        public CaregiverService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Caregiver>> GetAllCaregiversAsync()
        {
            return await _unitOfWork.CaregiversRepository.GetAllAsync();
        }

        public void CreateCaregiver(CaregiverCreateDTO caregiverDto)
        {

        }

    }
}
