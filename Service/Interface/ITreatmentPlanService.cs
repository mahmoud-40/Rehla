using BreastCancer.DTO.request;
using BreastCancer.DTO.response;

namespace BreastCancer.Service.Interface
{
    public interface ITreatmentPlanService
    {
        Task<TreatmentPlanResponseDTO> CreateTreatmentPlanAsync(string patientId, TreatmentPlanCreateDTO treatmentPlanDto);
        Task<TreatmentPlanResponseDTO> UpdateTreatmentPlanAsync(int id, string patientId, TreatmentPlanUpdateDTO treatmentPlanDto);
    }
}

