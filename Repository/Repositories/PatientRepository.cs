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
            return await _dbSet.FindAsync(id);
        }

        public async Task<IEnumerable<Patient>> GetPagedAsync(int pageNumber, int pageSize)
        {
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
