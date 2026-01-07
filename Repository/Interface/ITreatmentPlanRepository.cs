using BreastCancer.Models;

namespace BreastCancer.Repository.Interface
{
    public interface ITreatmentPlanRepository : IGenericRepository<TreatmentPlan>
    {
        Task<TreatmentPlan?> GetByIdAsync(int id);
    }
}

