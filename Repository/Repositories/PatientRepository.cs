using BreastCancer.Context;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Repository.Repositories
{
    public class PatientRepository : GenericRepository<Patient>, IPatientRepository
    {
        public PatientRepository(BreastCancerDB context) : base(context)
        {

        }

        public async Task<Patient?> GetByIdAsync(string id)
        {
            return await _dbSet
                .Include(p => p.User)
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.User)
                .Include(p => p.TreatmentPlan)
                .FirstOrDefaultAsync(p => p.UserId == id);
        }

        public async Task<IEnumerable<Patient>> GetPagedAsync(int pageNumber, int pageSize)
        {
            return await _dbSet
                .Include(p => p.User)
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.User)
                .Include(p => p.TreatmentPlan)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        public async Task<Patient?> GetByEmailAsync(string Email)
        {
            return await _dbSet.FirstOrDefaultAsync(e => e.User.Email == Email);
        }

        public async Task<Patient?> GetPatientWithTreatmentPlanAsync(string patientId)
        {
            var patient = await _dbSet
                .Include(p => p.TreatmentPlan)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == patientId);

            return patient;
        }

        //public async Task<IEnumerable<Patient>> GetAllWithDoctorAndCaregiverAsync()
        //{
        //    return await _dbSet.Include(p => p.Doctor).Include(p => p.Caregivers).ToListAsync();
        //}

        //public async Task<Patient> GetByIdWithDoctorAndCaregiverAsync(string id)
        //{
        //    return await _dbSet.Include(p => p.Doctor).Include(p => p.Caregivers).FirstOrDefaultAsync(p => p.Id == id);
        //}
    }
}
