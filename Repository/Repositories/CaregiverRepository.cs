using BreastCancer.Context;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Repository.Repositories
{
    public class CaregiverRepository : Repository<Caregiver> , ICaregiverRepository
    {
        public CaregiverRepository(ApplicationDbContext context) : base(context)
        {

        }

        public async Task<IEnumerable<Caregiver>> GetAllWithPatientAsync()
        {
            return await _dbSet.Include(c => c.Patient).ToListAsync();
        }

        public async Task<Caregiver> GetByIdWithPatientAsync(int id)
        {
            return await _dbSet.Include(c=> c.Id == id).FirstOrDefaultAsync(c=> c.Id == id);

        }
    }
}
