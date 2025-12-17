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

        public PatientService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ILogger<PatientService> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<PatientResponseDTO?> GetPatientByIdAsync(string id)
        {
            try
            {
                var patient = await _unitOfWork.PatientsRepository.GetByIdAsync(id);
                if (patient == null)
                {
                    return null;
                }

                return MapToResponseDTO(patient);
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
                var patients = await _unitOfWork.PatientsRepository.GetPagedAsync(pageNumber, pageSize);
                return patients.Select(MapToResponseDTO);
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

                var patient = new Patient
                {
                    FirstName = patientDto.FirstName,
                    LastName = patientDto.LastName,
                    Email = patientDto.Email,
                    UserName = patientDto.Email,
                    PhoneNumber = patientDto.PhoneNumber,
                    Address = patientDto.Address,
                    ImageUrl = patientDto.ImageUrl,
                    DateOfBirth = patientDto.DateOfBirth,
                    Gender = patientDto.Gender,
                    MedicalHistory = patientDto.MedicalHistory,
                    DoctorId = patientDto.DoctorId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(patient, patientDto.Password);
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
                return MapToResponseDTO(createdPatient!);
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
                return MapToResponseDTO(updatedPatient!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient with ID: {PatientId}", id);
                throw;
            }
        }

        public async Task<bool> DeletePatientAsync(string id)
        {
            try
            {
                var patient = await _unitOfWork.PatientsRepository.GetByIdAsync(id);
                if (patient == null)
                {
                    return false;
                }

                // Soft delete by setting IsActive to false
                patient.IsActive = false;
                patient.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.PatientsRepository.Update(patient);
                await _unitOfWork.PatientsRepository.SaveChangesAsync();

                _logger.LogInformation("Patient soft deleted successfully with ID: {PatientId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient with ID: {PatientId}", id);
                throw;
            }
        }

        private PatientResponseDTO MapToResponseDTO(Patient patient)
        {
            return new PatientResponseDTO
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                FullName = patient.FullName,
                Email = patient.Email ?? string.Empty,
                PhoneNumber = patient.PhoneNumber,
                Address = patient.Address,
                ImageUrl = patient.ImageUrl,
                DateOfBirth = patient.DateOfBirth,
                Age = patient.Age,
                Gender = patient.Gender,
                IsActive = patient.IsActive,
                MedicalHistory = patient.MedicalHistory,
                DoctorId = patient.DoctorId,
                DoctorName = patient.Doctor != null ? patient.Doctor.FullName : null,
                CreatedAt = patient.CreatedAt,
                UpdatedAt = patient.UpdatedAt
            };
        }
    }
}

