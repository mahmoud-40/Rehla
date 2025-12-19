using BreastCancer.Models;

namespace BreastCancer.Repository.Interface
{
    public interface IDoctorRepository : IGenericRepository<Doctor>
    {
        // Overload for string ID (Doctor uses string IDs from IdentityUser, while generic uses int)
        Task<Doctor?> GetByIdAsync(string id);
        //Task<IEnumerable<Doctor>> GetAllWithPatientsAsync();
        //Task<IEnumerable<Doctor>> GetBySpecializationAsync(string specialization);
    }
}
