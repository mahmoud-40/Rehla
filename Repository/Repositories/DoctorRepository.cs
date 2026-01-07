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

        public async Task<Doctor?> GetByIdAsync(string id)
        {
            // Rely on EF Core lazy loading proxies to load navigation properties (User, Patients)
            return await _dbSet.FirstOrDefaultAsync(d => d.UserId == id);
        }

        public async Task<IEnumerable<Doctor>> GetPagedAsync(int pageNumber, int pageSize)
        {
            // Rely on EF Core lazy loading proxies for navigation properties (User, Patients)
            // Pagination is applied only to the Doctor set
            return await _dbSet
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
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
