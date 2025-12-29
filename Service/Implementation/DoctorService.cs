using AutoMapper;
using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BreastCancer.Service.Implementation
{
    public class DoctorService : IDoctorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DoctorService> _logger;
        private readonly IMapper _mapper;

        public DoctorService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ILogger<DoctorService> logger,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<DoctorResponseDTO?> GetDoctorByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("Doctor ID is required.", nameof(id));
                }

                var doctor = await _unitOfWork.DoctorsRepository.GetByIdAsync(id);
                if (doctor == null)
                {
                    return null;
                }

                return _mapper.Map<DoctorResponseDTO>(doctor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving doctor with ID: {DoctorId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<DoctorResponseDTO>> GetAllDoctorsAsync(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1)
                {
                    throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));
                }

                var doctors = await _unitOfWork.DoctorsRepository.GetPagedAsync(pageNumber, pageSize);
                return _mapper.Map<IEnumerable<DoctorResponseDTO>>(doctors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all doctors");
                throw;
            }
        }

        public async Task<DoctorResponseDTO> CreateDoctorAsync(DoctorCreateDTO doctorDto)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(doctorDto.Email);
                if (existingUser != null)
                {
                    throw new InvalidOperationException($"A user with email '{doctorDto.Email}' already exists.");
                }

                // Generate temporary password
                var temporaryPassword = GenerateTemporaryPassword();

                // Step 1: Create ApplicationUser using AutoMapper
                var user = _mapper.Map<ApplicationUser>(doctorDto);

                var result = await _userManager.CreateAsync(user, temporaryPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create user: {errors}");
                }

                // Step 2: Assign Doctor role
                await _userManager.AddToRoleAsync(user, "Doctor");

                // Step 3: Create Doctor entity using AutoMapper
                var doctor = _mapper.Map<Doctor>(doctorDto);
                doctor.UserId = user.Id;

                _unitOfWork.DoctorsRepository.Add(doctor);
                await _unitOfWork.DoctorsRepository.SaveChangesAsync();

                // Load related data
                var createdDoctor = await _unitOfWork.DoctorsRepository.GetByIdAsync(user.Id);

                _logger.LogInformation("Doctor created successfully with ID: {DoctorId}", user.Id);
                return _mapper.Map<DoctorResponseDTO>(createdDoctor!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating doctor");
                throw;
            }
        }

        public async Task<DoctorResponseDTO?> UpdateDoctorAsync(string id, DoctorUpdateDTO doctorDto)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("Doctor ID is required.", nameof(id));
                }

                var doctor = await _unitOfWork.DoctorsRepository.GetByIdAsync(id);
                if (doctor == null || doctor.User == null)
                {
                    return null;
                }

                var user = doctor.User;

                // Validate email if being updated
                if (!string.IsNullOrEmpty(doctorDto.Email))
                {
                    var existingUser = await _userManager.FindByEmailAsync(doctorDto.Email);
                    if (existingUser != null && existingUser.Id != id)
                    {
                        throw new InvalidOperationException($"Email '{doctorDto.Email}' is already taken.");
                    }
                }

                // Update User properties using AutoMapper (only maps non-null properties)
                _mapper.Map(doctorDto, user);

                // Update Doctor-specific properties using AutoMapper (only maps non-null properties)
                _mapper.Map(doctorDto, doctor);

                // Update both User and Doctor
                await _userManager.UpdateAsync(user);
                _unitOfWork.DoctorsRepository.Update(doctor);
                await _unitOfWork.DoctorsRepository.SaveChangesAsync();

                var updatedDoctor = await _unitOfWork.DoctorsRepository.GetByIdAsync(id);
                _logger.LogInformation("Doctor updated successfully with ID: {DoctorId}", id);
                return _mapper.Map<DoctorResponseDTO>(updatedDoctor!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating doctor with ID: {DoctorId}", id);
                throw;
            }
        }

        public async Task DeleteDoctorAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("Doctor ID is required.", nameof(id));
                }

                var doctor = await _unitOfWork.DoctorsRepository.GetByIdAsync(id);
                if (doctor == null || doctor.User == null)
                {
                    throw new InvalidOperationException($"Doctor with ID '{id}' not found.");
                }

                // Soft delete by setting User.IsActive to false
                var user = doctor.User;
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;

                await _userManager.UpdateAsync(user);
                await _unitOfWork.DoctorsRepository.SaveChangesAsync();

                _logger.LogInformation("Doctor soft deleted successfully with ID: {DoctorId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting doctor with ID: {DoctorId}", id);
                throw;
            }
        }

        public async Task HardDeleteDoctorAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("Doctor ID is required.", nameof(id));
                }

                var doctor = await _unitOfWork.DoctorsRepository.GetByIdAsync(id);
                if (doctor == null || doctor.User == null)
                {
                    throw new InvalidOperationException($"Doctor with ID '{id}' not found.");
                }

                // Hard delete: Delete ApplicationUser (Doctor will be deleted via cascade or foreign key constraint)
                var user = doctor.User;
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to delete doctor: {errors}");
                }

                _logger.LogInformation("Doctor hard deleted successfully with ID: {DoctorId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hard deleting doctor with ID: {DoctorId}", id);
                throw;
            }
        }

        private string GenerateTemporaryPassword()
        {
            // Generate a secure temporary password that meets Identity requirements
            // Must have: uppercase, lowercase, digit, non-alphanumeric, min 8 chars
            const string upperCase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string specialChars = "!@#$%^&*";
            const int length = 12;

            var random = new Random();
            var password = new char[length];
            
            // Ensure at least one of each required character type
            password[0] = upperCase[random.Next(upperCase.Length)];
            password[1] = lowerCase[random.Next(lowerCase.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = specialChars[random.Next(specialChars.Length)];

            // Fill the rest randomly
            var allChars = upperCase + lowerCase + digits + specialChars;
            for (int i = 4; i < length; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            // Shuffle the password array
            for (int i = 0; i < length; i++)
            {
                var j = random.Next(i, length);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password);
        }
    }
}

