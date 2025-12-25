using AutoMapper;
using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Identity;

namespace BreastCancer.Service.Implementation
{
    public class CaregiverService : ICaregiverService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;

        public CaregiverService(IUnitOfWork unitOfWork, IMapper mapper, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
        }

        public async Task<IEnumerable<CaregiverResponse>> GetAllCaregivers()
        {
            var caregivers = await _unitOfWork.CaregiversRepository.FilterAsync(c => c.User.IsActive);

            if (caregivers == null || !caregivers.Any())
            {
                return Enumerable.Empty<CaregiverResponse>();
            }

            return _mapper.Map<IEnumerable<CaregiverResponse>>(caregivers);
        }

        public Task<CaregiverResponse> GetCaregiverById(string id)
        {
            var caregiver = getById(id);

            var caregiverResponse = _mapper.Map<CaregiverResponse>(caregiver);
            return Task.FromResult(caregiverResponse);
        }

        public Task<IEnumerable<CaregiverResponse>> GetCaregiverByPatientId(string patientId)
        {
            var caregivers =  _unitOfWork.CaregiversRepository
                .FilterAsync(c => c.PatientId == patientId && c.User.IsActive).Result;

            if (caregivers == null || !caregivers.Any())
            {
                return Task.FromResult(Enumerable.Empty<CaregiverResponse>());
            }

            var caregiverResponses = _mapper.Map<IEnumerable<CaregiverResponse>>(caregivers);
            return Task.FromResult(caregiverResponses);
        }

        public void CreateCaregiver(CaregiverCreateDTO caregiverDto)
        {
            var existingUser = _userManager.FindByEmailAsync(caregiverDto.Email).Result;
            if (existingUser != null)
            {
                throw new Exception("User with the same email already exists.");
            }

            ApplicationUser user = new ApplicationUser
            {
                UserName = caregiverDto.Email,
                Email = caregiverDto.Email,
                FirstName = caregiverDto.FirstName,
                LastName = caregiverDto.LastName
            };

            _userManager.CreateAsync(user, "DefaultPassword123!").Wait();
            Caregiver caregiver = _mapper.Map<Caregiver>(caregiverDto);
            caregiver.UserId = user.Id;
            _unitOfWork.CaregiversRepository.Add(caregiver);
            _unitOfWork.Save();
        }

        public async Task UpdateCaregiver(string userId, CaregiverUpdateDTO updateDto)
        {
            var user = getUserById(userId).Result;

            _mapper.Map(updateDto, user);

            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new Exception($"Update failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        public void DeleteCaregiver(string id)
        {
            var caregiver = getById(id);
            caregiver.User.IsActive = false;
            _unitOfWork.CaregiversRepository.Update(caregiver);
            _unitOfWork.Save();
        }

        public void HardDeleteCaregiverById(string id)
        {
            var caregiver = getById(id);
            _unitOfWork.CaregiversRepository.Delete(caregiver);
            _unitOfWork.Save();
        }

        private Caregiver getById(string id)
        {
            var caregiver =  _unitOfWork.CaregiversRepository.GetByIdAsync(id).Result;
            if (caregiver == null)
            {
                throw new Exception("Caregiver not found.");
            }

            return caregiver;
        }

        private async Task<ApplicationUser> getUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            return user;
        }
    }
}
