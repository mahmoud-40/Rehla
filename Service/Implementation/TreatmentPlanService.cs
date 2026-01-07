using AutoMapper;
using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Enum;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using BreastCancer.Service.Interface;
using Microsoft.Extensions.Logging;

namespace BreastCancer.Service.Implementation
{
    public class TreatmentPlanService : ITreatmentPlanService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<TreatmentPlanService> _logger;

        public TreatmentPlanService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<TreatmentPlanService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<TreatmentPlanResponseDTO> CreateTreatmentPlanAsync(string patientId, TreatmentPlanCreateDTO treatmentPlanDto)
        {
            try
            {
                // Validate patient exists
                var patient = await _unitOfWork.PatientsRepository.GetByIdAsync(patientId);
                if (patient == null)
                {
                    throw new InvalidOperationException($"Patient with ID '{patientId}' not found.");
                }

                // Validate doctor if DoctorId is provided
                if (!string.IsNullOrEmpty(treatmentPlanDto.DoctorId))
                {
                    var doctor = await _unitOfWork.DoctorsRepository.GetByIdAsync(treatmentPlanDto.DoctorId);
                    if (doctor == null)
                    {
                        throw new InvalidOperationException($"Doctor with ID '{treatmentPlanDto.DoctorId}' not found.");
                    }
                }

                // Map DTO to entity
                var treatmentPlan = _mapper.Map<TreatmentPlan>(treatmentPlanDto);
                treatmentPlan.PatientId = patientId;
                treatmentPlan.Status = TreatmentPlanStatus.NotStarted;
                treatmentPlan.CreatedAt = DateTime.UtcNow;

                // Calculate initial NextAlert for each medicine (set to StartTime)
                foreach (var medicine in treatmentPlan.Medicines)
                {
                    medicine.NextAlert = medicine.StartTime;
                    medicine.CreatedAt = DateTime.UtcNow;
                }

                // Add to repository
                await _unitOfWork.TreatmentPlansRepository.AddAsync(treatmentPlan);
                await _unitOfWork.SaveAsync();

                // Reload for response - lazy loading will load Medicines automatically
                var createdPlan = await _unitOfWork.TreatmentPlansRepository.GetByIdAsync(treatmentPlan.Id);
                return _mapper.Map<TreatmentPlanResponseDTO>(createdPlan ?? treatmentPlan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating treatment plan for patient: {PatientId}", patientId);
                throw;
            }
        }

        public async Task<TreatmentPlanResponseDTO> UpdateTreatmentPlanAsync(int id, string patientId, TreatmentPlanUpdateDTO treatmentPlanDto)
        {
            try
            {
                // Get existing treatment plan
                var existingPlan = await _unitOfWork.TreatmentPlansRepository.GetByIdAsync(id);
                if (existingPlan == null)
                {
                    throw new InvalidOperationException($"Treatment plan with ID '{id}' not found.");
                }

                // Verify patient owns this treatment plan (or is admin)
                if (existingPlan.PatientId != patientId)
                {
                    throw new UnauthorizedAccessException("You do not have permission to update this treatment plan.");
                }

                // Validate doctor if DoctorId is provided and changed
                if (!string.IsNullOrEmpty(treatmentPlanDto.DoctorId) && treatmentPlanDto.DoctorId != existingPlan.DoctorId)
                {
                    var doctor = await _unitOfWork.DoctorsRepository.GetByIdAsync(treatmentPlanDto.DoctorId);
                    if (doctor == null)
                    {
                        throw new InvalidOperationException($"Doctor with ID '{treatmentPlanDto.DoctorId}' not found.");
                    }
                }

                // Use AutoMapper to update only non-null properties
                _mapper.Map(treatmentPlanDto, existingPlan);
                
                existingPlan.UpdatedAt = DateTime.UtcNow;
                // TODO: Set UpdatedBy from authenticated user context

                // Handle medicines update
                if (treatmentPlanDto.Medicines != null)
                {
                    // Load existing medicines (lazy loading will load them)
                    var existingMedicines = existingPlan.Medicines.ToList();

                    foreach (var medicineDto in treatmentPlanDto.Medicines)
                    {
                        if (medicineDto.Id.HasValue && medicineDto.Id.GetValueOrDefault() > 0)
                        {
                            // Update existing medicine using AutoMapper
                            var medicineId = medicineDto.Id.Value;
                            var existingMedicine = existingMedicines.FirstOrDefault(m => m.Id == medicineId);
                            if (existingMedicine != null)
                            {
                                // Store previous values to detect changes
                                var previousLastTaken = existingMedicine.LastTaken;
                                var previousIntervalHours = existingMedicine.IntervalHours;
                                
                                // AutoMapper will only map non-null properties (conditional mapping)
                                _mapper.Map(medicineDto, existingMedicine);
                                
                                // Dynamic scheduling: Recalculate NextAlert if LastTaken or IntervalHours was updated
                                var lastTakenWasUpdated = medicineDto.LastTaken.HasValue && medicineDto.LastTaken != previousLastTaken;
                                var intervalWasUpdated = medicineDto.IntervalHours.HasValue && medicineDto.IntervalHours.Value != previousIntervalHours;
                                
                                if (lastTakenWasUpdated || intervalWasUpdated)
                                {
                                    // Use helper method to recalculate NextAlert
                                    RecalculateNextAlert(existingMedicine);
                                }
                                
                                existingMedicine.UpdatedAt = DateTime.UtcNow;
                                // TODO: Set UpdatedBy from authenticated user context
                            }
                        }
                        else
                        {
                            // Create new medicine using AutoMapper
                            var newMedicine = _mapper.Map<Medicine>(medicineDto);
                            newMedicine.TreatmentPlanId = existingPlan.Id;
                            newMedicine.NextAlert = newMedicine.StartTime;
                            newMedicine.CreatedAt = DateTime.UtcNow;
                            // TODO: Set CreatedBy from authenticated user context

                            existingPlan.Medicines.Add(newMedicine);
                        }
                    }

                    // Remove medicines that are not in the update DTO
                    var medicineIdsInDto = treatmentPlanDto.Medicines
                        .Where(m => m.Id.HasValue && m.Id.GetValueOrDefault() > 0)
                        .Select(m => m.Id!.Value)
                        .ToList();

                    var medicinesToDelete = existingMedicines
                        .Where(m => !medicineIdsInDto.Contains(m.Id))
                        .ToList();

                    foreach (var medicineToDelete in medicinesToDelete)
                    {
                        existingPlan.Medicines.Remove(medicineToDelete);
                    }
                }

                // Update entity
                _unitOfWork.TreatmentPlansRepository.Update(existingPlan);
                await _unitOfWork.SaveAsync();

                // Reload for response - lazy loading will load Medicines automatically
                var updatedPlan = await _unitOfWork.TreatmentPlansRepository.GetByIdAsync(id);
                return _mapper.Map<TreatmentPlanResponseDTO>(updatedPlan ?? existingPlan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating treatment plan with ID: {TreatmentPlanId} for patient: {PatientId}", id, patientId);
                throw;
            }
        }

        public async Task<MedicineResponseDTO> MarkMedicineAsTakenAsync(int medicineId, string patientId)
        {
            try
            {
                // Get medicine with its treatment plan
                var medicine = await _unitOfWork.TreatmentPlansRepository.GetMedicineByIdAsync(medicineId);
                if (medicine == null)
                {
                    throw new InvalidOperationException($"Medicine with ID '{medicineId}' not found.");
                }

                // Verify patient owns the treatment plan
                if (medicine.TreatmentPlan?.PatientId != patientId)
                {
                    throw new UnauthorizedAccessException("You do not have permission to mark this medicine as taken.");
                }

                // Check if medicine has ended
                if (medicine.EndTime.HasValue && DateTime.UtcNow > medicine.EndTime.Value)
                {
                    throw new InvalidOperationException($"Medicine '{medicine.Name}' has already ended on {medicine.EndTime.Value:yyyy-MM-dd HH:mm} UTC.");
                }

                // Record the current timestamp as last_taken
                var currentTime = DateTime.UtcNow;
                medicine.LastTaken = currentTime;

                // Dynamically recalculate NextAlert = LastTaken + IntervalHours
                RecalculateNextAlert(medicine);

                medicine.UpdatedAt = currentTime;
                // TODO: Set UpdatedBy from authenticated user context

                // Save changes - EF Core change tracking will detect Medicine updates automatically
                await _unitOfWork.SaveAsync();

                // Return updated medicine
                return _mapper.Map<MedicineResponseDTO>(medicine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking medicine {MedicineId} as taken for patient: {PatientId}", medicineId, patientId);
                throw;
            }
        }

        /// <summary>
        /// Helper method to recalculate NextAlert based on LastTaken and IntervalHours
        /// </summary>
        private void RecalculateNextAlert(Medicine medicine)
        {
            if (!medicine.LastTaken.HasValue)
            {
                return; // Cannot recalculate without LastTaken
            }

            var lastTakenTime = medicine.LastTaken.Value;

            // Check if medicine has ended
            if (medicine.EndTime.HasValue && DateTime.UtcNow > medicine.EndTime.Value)
            {
                medicine.NextAlert = null; // Medicine has ended
                return;
            }

            // Calculate NextAlert = LastTaken + IntervalHours
            var nextAlertTime = lastTakenTime.AddHours(medicine.IntervalHours);

            // If medicine has an EndTime, ensure NextAlert doesn't exceed it
            if (medicine.EndTime.HasValue && nextAlertTime > medicine.EndTime.Value)
            {
                medicine.NextAlert = null; // No more alerts after end time
            }
            else
            {
                medicine.NextAlert = nextAlertTime;
            }
        }
    }
}

