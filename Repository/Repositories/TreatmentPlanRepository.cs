using BreastCancer.Context;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Repository.Repositories
{
    public class TreatmentPlanRepository : GenericRepository<TreatmentPlan>, ITreatmentPlanRepository
    {
        public TreatmentPlanRepository(BreastCancerDB context) : base(context)
        {
        }

        public async Task<TreatmentPlan?> GetByIdAsync(int id)
        {
            // Rely on EF Core lazy loading proxies to load navigation properties (Medicines, Patient, Doctor)
            return await _dbSet.FirstOrDefaultAsync(tp => tp.Id == id);
        }

        public async Task<Medicine?> GetMedicineByIdAsync(int medicineId)
        {
            // Rely on EF Core lazy loading proxies to load navigation properties (TreatmentPlan)
            var medicines = _Context.Set<Medicine>();
            return await medicines.FirstOrDefaultAsync(m => m.Id == medicineId);
        }
    }
}

