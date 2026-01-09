using BreastCancer.DTO.request;
using BreastCancer.DTO.response;

namespace BreastCancer.Service.Interface
{
    public interface ITreatmentPlanService
    {
        Task<TreatmentPlanResponseDTO> GetTreatmentPlanByIdAsync(int id);
        Task<TreatmentPlanResponseDTO> GetTreatmentPlanByPatientIdAsync(string id);
        Task<TreatmentPlanResponseDTO> CreateTreatmentPlanAsync(string patientId, TreatmentPlanCreateDTO treatmentPlanDto);
        Task<TreatmentPlanResponseDTO> UpdateTreatmentPlanAsync(int id, string patientId, TreatmentPlanUpdateDTO treatmentPlanDto);
        Task<MedicineResponseDTO> MarkMedicineAsTakenAsync(int medicineId, string patientId);


    }
}

