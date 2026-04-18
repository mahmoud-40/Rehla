using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Service.Implementation
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public AdminService(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<AdminUserResponseDTO>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var result = new List<AdminUserResponseDTO>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(MapUser(user, roles));
            }

            return result;
        }

        public async Task<IEnumerable<AdminUserResponseDTO>> GetUsersByRoleAsync(string role)
        {
            var users = await _userManager.GetUsersInRoleAsync(role);
            var result = new List<AdminUserResponseDTO>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(MapUser(user, roles));
            }

            return result;
        }

        public async Task DisableUserAsync(AdminDisableUserDTO request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }

        public async Task DeleteUserAsync(AdminDeleteUserDTO request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            var userType = request.UserType.Trim().ToLowerInvariant();
            switch (userType)
            {
                case "doctor":
                    var doctor = await _unitOfWork.DoctorsRepository.GetByIdAsync(request.UserId);
                    if (doctor != null)
                    {
                        _unitOfWork.DoctorsRepository.Delete(doctor);
                    }
                    break;
                case "patient":
                    var patient = await _unitOfWork.PatientsRepository.GetByIdAsync(request.UserId);
                    if (patient != null)
                    {
                        _unitOfWork.PatientsRepository.Delete(patient);
                    }
                    break;
                case "caregiver":
                    var caregiver = await _unitOfWork.CaregiversRepository.GetByIdAsync(request.UserId);
                    if (caregiver != null)
                    {
                        _unitOfWork.CaregiversRepository.Delete(caregiver);
                    }
                    break;
                case "admin":
                    break;
                default:
                    throw new Exception("Invalid user type.");
            }

            await _unitOfWork.SaveAsync();
            await _userManager.DeleteAsync(user);
        }

        public async Task AssignDoctorToPatientAsync(AssignDoctorToPatientDTO request)
        {
            var doctor = await _unitOfWork.DoctorsRepository.GetByIdAsync(request.DoctorId);
            if (doctor == null)
            {
                throw new Exception("Doctor not found.");
            }

            var patient = await _unitOfWork.PatientsRepository.GetByIdAsync(request.PatientId);
            if (patient == null)
            {
                throw new Exception("Patient not found.");
            }

            patient.DoctorId = request.DoctorId;
            _unitOfWork.PatientsRepository.Update(patient);
            await _unitOfWork.SaveAsync();
        }

        public async Task AssignCaregiverToPatientAsync(AssignCaregiverToPatientDTO request)
        {
            var caregiver = await _unitOfWork.CaregiversRepository.GetByIdAsync(request.CaregiverId);
            if (caregiver == null)
            {
                throw new Exception("Caregiver not found.");
            }

            var patient = await _unitOfWork.PatientsRepository.GetByIdAsync(request.PatientId);
            if (patient == null)
            {
                throw new Exception("Patient not found.");
            }

            caregiver.PatientId = request.PatientId;
            _unitOfWork.CaregiversRepository.Update(caregiver);
            await _unitOfWork.SaveAsync();
        }

        private static AdminUserResponseDTO MapUser(ApplicationUser user, IList<string> roles)
        {
            return new AdminUserResponseDTO
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Roles = roles.ToList()
            };
        }
    }
}

