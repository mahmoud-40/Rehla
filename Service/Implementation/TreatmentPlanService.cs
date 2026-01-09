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

        public async Task<TreatmentPlanResponseDTO> GetTreatmentPlanByIdAsync(int id)
        {
            try
            {
                var treatmentPlan = await _unitOfWork.TreatmentPlansRepository.GetByIdAsync(id) ?? throw new InvalidOperationException($"Treatment plan with ID '{id}' not found.");

                var treatmentPlanDto = _mapper.Map<TreatmentPlanResponseDTO>(treatmentPlan);

                return treatmentPlanDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting treatment plan: {id}", id);
                throw;
            }
        }

        public async Task<TreatmentPlanResponseDTO> GetTreatmentPlanByPatientIdAsync(string patientId)
        {
            try
            {
                var patient = await _unitOfWork.PatientsRepository.GetByIdAsync(patientId);
                if (patient == null)
                {
                    throw new InvalidOperationException($"Patient with ID '{patientId}' not found.");
                }

                if (!patient.TreatmentPlanId.HasValue)
                {
                    throw new InvalidOperationException($"Patient '{patientId}' does not have a treatment plan assigned.");
                }

                var treatmentPlan = await _unitOfWork.TreatmentPlansRepository
                    .GetByIdAsync(patient.TreatmentPlanId.Value);

                if (treatmentPlan == null)
                {
                    throw new InvalidOperationException(
                        $"Treatment plan with ID '{patient.TreatmentPlanId}' not found, but patient has it assigned.");
                }

                var treatmentPlanDto = _mapper.Map<TreatmentPlanResponseDTO>(treatmentPlan);

                return treatmentPlanDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting treatment plan for patient: {PatientId}", patientId);
                throw;
            }
        }

        public async Task<TreatmentPlanResponseDTO> CreateTreatmentPlanAsync(string patientId, TreatmentPlanCreateDTO treatmentPlanDto)
        {
            try
            {
                var patient = await _unitOfWork.PatientsRepository.GetByIdAsync(patientId);
                if (patient == null)
                {
                    throw new InvalidOperationException($"Patient with ID '{patientId}' not found.");
                }
                if (!string.IsNullOrEmpty(treatmentPlanDto.DoctorId))
                {
                    var doctor = await _unitOfWork.DoctorsRepository.GetByIdAsync(treatmentPlanDto.DoctorId);
                    if (doctor == null)
                    {
                        throw new InvalidOperationException($"Doctor with ID '{treatmentPlanDto.DoctorId}' not found.");
                    }
                }
                var treatmentPlan = _mapper.Map<TreatmentPlan>(treatmentPlanDto);
                treatmentPlan.PatientId = patientId;
                treatmentPlan.Status = TreatmentPlanStatus.NotStarted;
                treatmentPlan.CreatedAt = DateTime.UtcNow;
                foreach (var medicine in treatmentPlan.Medicines)
                {
                    medicine.NextAlert = medicine.StartTime;
                    medicine.CreatedAt = DateTime.UtcNow;
                }

                // TODO: fix performance issue
                await _unitOfWork.TreatmentPlansRepository.AddAsync(treatmentPlan);
                await _unitOfWork.SaveAsync();

                patient.TreatmentPlanId = treatmentPlan.Id;
                _unitOfWork.PatientsRepository.Update(patient);

                await _unitOfWork.SaveAsync();
                return _mapper.Map<TreatmentPlanResponseDTO>(treatmentPlan);
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
                var existingPlan = await _unitOfWork.TreatmentPlansRepository.GetByIdAsync(id);
                if (existingPlan == null)
                {
                    throw new InvalidOperationException($"Treatment plan with ID '{id}' not found.");
                }
                if (existingPlan.PatientId != patientId)
                {
                    throw new UnauthorizedAccessException("You do not have permission to update this treatment plan.");
                }
                if (!string.IsNullOrEmpty(treatmentPlanDto.DoctorId) && treatmentPlanDto.DoctorId != existingPlan.DoctorId)
                {
                    var doctor = await _unitOfWork.DoctorsRepository.GetByIdAsync(treatmentPlanDto.DoctorId);
                    if (doctor == null)
                    {
                        throw new InvalidOperationException($"Doctor with ID '{treatmentPlanDto.DoctorId}' not found.");
                    }
                }
                _mapper.Map(treatmentPlanDto, existingPlan);

                existingPlan.UpdatedAt = DateTime.UtcNow;
                // TODO: Set UpdatedBy from authenticated user context
                if (treatmentPlanDto.Medicines != null)
                {
                    var existingMedicines = existingPlan.Medicines.ToList();

                    foreach (var medicineDto in treatmentPlanDto.Medicines)
                    {
                        if (medicineDto.Id.HasValue && medicineDto.Id.GetValueOrDefault() > 0)
                        {
                            var medicineId = medicineDto.Id.Value;
                            var existingMedicine = existingMedicines.FirstOrDefault(m => m.Id == medicineId);
                            if (existingMedicine != null)
                            {
                                var previousLastTaken = existingMedicine.LastTaken;
                                var previousIntervalHours = existingMedicine.IntervalHours;


                                _mapper.Map(medicineDto, existingMedicine);

                                var lastTakenWasUpdated = medicineDto.LastTaken.HasValue && medicineDto.LastTaken != previousLastTaken;
                                var intervalWasUpdated = medicineDto.IntervalHours.HasValue && medicineDto.IntervalHours.Value != previousIntervalHours;

                                if (lastTakenWasUpdated || intervalWasUpdated)
                                {
                                    RecalculateNextAlert(existingMedicine);
                                }

                                existingMedicine.UpdatedAt = DateTime.UtcNow;
                                // TODO: Set UpdatedBy from authenticated user context
                            }
                        }
                        else
                        {
                            var newMedicine = _mapper.Map<Medicine>(medicineDto);
                            newMedicine.TreatmentPlanId = existingPlan.Id;
                            newMedicine.NextAlert = newMedicine.StartTime;
                            newMedicine.CreatedAt = DateTime.UtcNow;
                            // TODO: Set CreatedBy from authenticated user context

                            existingPlan.Medicines.Add(newMedicine);
                        }
                    }
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

                _unitOfWork.TreatmentPlansRepository.Update(existingPlan);
                await _unitOfWork.SaveAsync();
                return _mapper.Map<TreatmentPlanResponseDTO>(existingPlan);
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
                var medicine = await _unitOfWork.TreatmentPlansRepository.GetMedicineByIdAsync(medicineId);
                if (medicine == null)
                {
                    throw new InvalidOperationException($"Medicine with ID '{medicineId}' not found.");
                }
                if (medicine.TreatmentPlan?.PatientId != patientId)
                {
                    throw new UnauthorizedAccessException("You do not have permission to mark this medicine as taken.");
                }
                if (medicine.EndTime.HasValue && DateTime.UtcNow > medicine.EndTime.Value)
                {
                    throw new InvalidOperationException($"Medicine '{medicine.Name}' has already ended on {medicine.EndTime.Value:yyyy-MM-dd HH:mm} UTC.");
                }

                var currentTime = DateTime.UtcNow;
                medicine.LastTaken = currentTime;


                RecalculateNextAlert(medicine);

                medicine.UpdatedAt = currentTime;
                // TODO: Set UpdatedBy from authenticated user context

                await _unitOfWork.SaveAsync();

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
                return;
            }

            var lastTakenTime = medicine.LastTaken.Value;

            if (medicine.EndTime.HasValue && DateTime.UtcNow > medicine.EndTime.Value)
            {
                medicine.NextAlert = null;
                return;
            }
            medicine.NextAlert = lastTakenTime.AddHours(medicine.IntervalHours);
        }
    }
}

