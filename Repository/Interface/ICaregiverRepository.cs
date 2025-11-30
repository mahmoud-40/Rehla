using BreastCancer.Models;

namespace BreastCancer.Repository.Interface
{
    public interface ICaregiverRepository : IRepository<Caregiver>
    {
        Task<IEnumerable<Caregiver>> GetAllWithPatientAsync();
        Task<Caregiver> GetByIdWithPatientAsync(int id);

    }
}
