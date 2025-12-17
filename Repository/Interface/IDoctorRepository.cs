using BreastCancer.Models;

namespace BreastCancer.Repository.Interface
{
    public interface IDoctorRepository : IGenericRepository<Doctor>
    {
        Task<Doctor?> GetByIdAsync(string id);
        //Task<IEnumerable<Doctor>> GetAllWithPatientsAsync();
        //Task<IEnumerable<Doctor>> GetBySpecializationAsync(string specialization);
    }
}
