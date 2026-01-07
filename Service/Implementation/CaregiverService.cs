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

        public async Task<CaregiverResponse> GetCaregiverById(string id)
        {
            var caregiver = await GetByIdAsync(id);

            var caregiverResponse = _mapper.Map<CaregiverResponse>(caregiver);
            return caregiverResponse;
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

        public async Task CreateCaregiver(CaregiverCreateDTO caregiverDto)
        {
            var existingUser = await _userManager.FindByEmailAsync(caregiverDto.Email);
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

            var randomPassword = Guid.NewGuid().ToString().Substring(0, 8) + "aA1!";

            await _userManager.CreateAsync(user, randomPassword);
            await _userManager.AddToRoleAsync(user, "Caregiver");
            Caregiver caregiver = _mapper.Map<Caregiver>(caregiverDto);
            caregiver.UserId = user.Id;
            await _unitOfWork.CaregiversRepository.AddAsync(caregiver);
            await _unitOfWork.SaveAsync();
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

        public async Task DeleteCaregiver(string id)
        {
            var caregiver = await GetByIdAsync(id);
            caregiver.User.IsActive = false;
            _unitOfWork.CaregiversRepository.Update(caregiver);
            await _unitOfWork.SaveAsync();
        }

        public async Task HardDeleteCaregiverById(string id)
        {
            var caregiver = await GetByIdAsync(id);
            _unitOfWork.CaregiversRepository.Delete(caregiver);
            await _unitOfWork.SaveAsync();
        }

        private async Task<Caregiver> GetByIdAsync(string id)
        {
            var caregiver = await _unitOfWork.CaregiversRepository.GetByIdAsync(id);
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
