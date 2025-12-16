using BreastCancer.Context;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Repository.Repositories
{
    public class DoctorRepository :GenericRepository<Doctor> ,IDoctorRepository
    {
        public DoctorRepository(BreastCancerDB context):base(context)
        {

        }

        //public async Task<IEnumerable<Doctor>> GetAllWithPatientsAsync()
        //{
        //    return await _dbSet.Include(d => d.Patients).ToListAsync();
        //}

        //public async Task<IEnumerable<Doctor>> GetBySpecializationAsync(string specialization)
        //{
        //    return await _dbSet.Where(d => d.Specialization == specialization).ToListAsync();
        //}
    }
}
