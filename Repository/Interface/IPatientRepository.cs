using BreastCancer.Models;

namespace BreastCancer.Repository.Interface
{
    public interface IPatientRepository : IGenericRepository<Patient>
    {
        Task<Patient?> GetByIdAsync(string id);
        Task<IEnumerable<Patient>> GetPagedAsync(int pageNumber, int pageSize);
        //Task<IEnumerable<Patient>> GetAllWithDoctorAndCaregiverAsync();
        //Task<Patient> GetByIdWithDoctorAndCaregiverAsync(string id);
    }
}
