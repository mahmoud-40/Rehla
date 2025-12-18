using AutoMapper;
using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BreastCancer.Service.Implementation
{
    public class PatientService : IPatientService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PatientService> _logger;
        private readonly IMapper _mapper;

        public PatientService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ILogger<PatientService> logger,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<PatientResponseDTO?> GetPatientByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("Patient ID is required.", nameof(id));
                }

                var patient = await _unitOfWork.PatientsRepository.GetByIdAsync(id);
                if (patient == null)
                {
                    return null;
                }

                return _mapper.Map<PatientResponseDTO>(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient with ID: {PatientId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<PatientResponseDTO>> GetAllPatientsAsync(int pageNumber = 1, int pageSize = 10)
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

                var patients = await _unitOfWork.PatientsRepository.GetPagedAsync(pageNumber, pageSize);
                return _mapper.Map<IEnumerable<PatientResponseDTO>>(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all patients");
                throw;
            }
        }

        public async Task<PatientResponseDTO> CreatePatientAsync(PatientCreateDTO patientDto)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(patientDto.Email);
                if (existingUser != null)
                {
                    throw new InvalidOperationException($"A user with email '{patientDto.Email}' already exists.");
                }

                // Validate doctor exists if DoctorId is provided
                if (!string.IsNullOrEmpty(patientDto.DoctorId))
                {
                    var doctor = await _unitOfWork.DoctorsRepository.GetByIdAsync(patientDto.DoctorId);
                    if (doctor == null)
                    {
                        throw new InvalidOperationException($"Doctor with ID '{patientDto.DoctorId}' not found.");
                    }
                }

                // Generate temporary password (always generate since Password is not in DTO)
                var temporaryPassword = GenerateTemporaryPassword();

                var patient = new Patient
                {
                    FirstName = patientDto.FirstName,
                    LastName = patientDto.LastName,
                    Email = patientDto.Email,
                    UserName = patientDto.Email,
                    PhoneNumber = patientDto.PhoneNumber,
                    Address = patientDto.Address,
                    ImageUrl = patientDto.ImageUrl,
                    DateOfBirth = patientDto.DateOfBirth ?? DateTime.UtcNow.AddYears(-30), // Default to 30 years ago if not provided
                    Gender = patientDto.Gender ?? Enum.Gender.PreferNotToSay, // Default gender if not provided
                    MedicalHistory = patientDto.MedicalHistory,
                    DoctorId = patientDto.DoctorId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(patient, temporaryPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create patient: {errors}");
                }

                // Assign Patient role
                await _userManager.AddToRoleAsync(patient, "Patient");

                // Load related data
                await _unitOfWork.PatientsRepository.SaveChangesAsync();
                var createdPatient = await _unitOfWork.PatientsRepository.GetByIdAsync(patient.Id);

                _logger.LogInformation("Patient created successfully with ID: {PatientId}", patient.Id);
                return _mapper.Map<PatientResponseDTO>(createdPatient!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient");
                throw;
            }
        }

        public async Task<PatientResponseDTO?> UpdatePatientAsync(string id, PatientUpdateDTO patientDto)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("Patient ID is required.", nameof(id));
                }

                var patient = await _unitOfWork.PatientsRepository.GetByIdAsync(id);
                if (patient == null)
                {
                    return null;
                }

                // Update properties if provided
                if (!string.IsNullOrEmpty(patientDto.FirstName))
                    patient.FirstName = patientDto.FirstName;

                if (!string.IsNullOrEmpty(patientDto.LastName))
                    patient.LastName = patientDto.LastName;

                if (!string.IsNullOrEmpty(patientDto.Email))
                {
                    // Check if email is already taken by another user
                    var existingUser = await _userManager.FindByEmailAsync(patientDto.Email);
                    if (existingUser != null && existingUser.Id != id)
                    {
                        throw new InvalidOperationException($"Email '{patientDto.Email}' is already taken.");
                    }
                    patient.Email = patientDto.Email;
                    patient.UserName = patientDto.Email;
                }

                if (!string.IsNullOrEmpty(patientDto.PhoneNumber))
                    patient.PhoneNumber = patientDto.PhoneNumber;

                if (patientDto.Address != null)
                    patient.Address = patientDto.Address;

                if (patientDto.ImageUrl != null)
                    patient.ImageUrl = patientDto.ImageUrl;

                if (patientDto.DateOfBirth.HasValue)
                    patient.DateOfBirth = patientDto.DateOfBirth.Value;

                if (patientDto.Gender.HasValue)
                    patient.Gender = patientDto.Gender.Value;

                if (patientDto.MedicalHistory != null)
                    patient.MedicalHistory = patientDto.MedicalHistory;

                if (!string.IsNullOrEmpty(patientDto.DoctorId))
                {
                    // Validate doctor exists
                    var doctor = await _unitOfWork.DoctorsRepository.GetByIdAsync(patientDto.DoctorId);
                    if (doctor == null)
                    {
                        throw new InvalidOperationException($"Doctor with ID '{patientDto.DoctorId}' not found.");
                    }
                    patient.DoctorId = patientDto.DoctorId;
                }

                if (patientDto.IsActive.HasValue)
                    patient.IsActive = patientDto.IsActive.Value;

                patient.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.PatientsRepository.Update(patient);
                await _unitOfWork.PatientsRepository.SaveChangesAsync();

                var updatedPatient = await _unitOfWork.PatientsRepository.GetByIdAsync(id);
                _logger.LogInformation("Patient updated successfully with ID: {PatientId}", id);
                return _mapper.Map<PatientResponseDTO>(updatedPatient!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient with ID: {PatientId}", id);
                throw;
            }
        }

        public async Task DeletePatientAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("Patient ID is required.", nameof(id));
                }

                var patient = await _unitOfWork.PatientsRepository.GetByIdAsync(id);
                if (patient == null)
                {
                    throw new InvalidOperationException($"Patient with ID '{id}' not found.");
                }

                // Soft delete by setting IsActive to false
                patient.IsActive = false;
                patient.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.PatientsRepository.Update(patient);
                await _unitOfWork.PatientsRepository.SaveChangesAsync();

                _logger.LogInformation("Patient soft deleted successfully with ID: {PatientId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient with ID: {PatientId}", id);
                throw;
            }
        }

        public async Task HardDeletePatientAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("Patient ID is required.", nameof(id));
                }

                var patient = await _unitOfWork.PatientsRepository.GetByIdAsync(id);
                if (patient == null)
                {
                    throw new InvalidOperationException($"Patient with ID '{id}' not found.");
                }

                // Hard delete: Remove from Identity and database
                var result = await _userManager.DeleteAsync(patient);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to delete patient: {errors}");
                }

                _logger.LogInformation("Patient hard deleted successfully with ID: {PatientId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hard deleting patient with ID: {PatientId}", id);
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

