using BreastCancer.Models;

namespace BreastCancer.Repository.Interface
{
    public interface IPatientRepository :IRepository<Patient>
    {

        Task<IEnumerable<Patient>> GetAllWithDoctorAndCaregiverAsync();
        Task<Patient> GetByIdWithDoctorAndCaregiverAsync(int id);

    }
}
