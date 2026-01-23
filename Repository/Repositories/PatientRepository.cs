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
            // Rely on EF Core lazy loading proxies to load navigation properties (User, Doctor, Caregivers)
            return await _dbSet.FirstOrDefaultAsync(p => p.UserId == id);
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

        public async Task<IEnumerable<Patient>> GetPagedAsync(int pageNumber, int pageSize)
        {
            // Rely on EF Core lazy loading proxies for navigation properties (User, Doctor, Caregivers)
            // Pagination is applied only to the Patient set
            return await _dbSet
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
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
