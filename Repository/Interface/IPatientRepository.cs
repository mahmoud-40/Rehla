using BreastCancer.Models;

namespace BreastCancer.Repository.Interface
{
    public interface IPatientRepository : IGenericRepository<Patient>
    {
        // Overload for string ID (Patient uses string IDs from IdentityUser, while generic uses int)
        Task<Patient?> GetByIdAsync(string id);
        Task<Patient?> GetByEmailAsync(string Email);
        Task<IEnumerable<Patient>> GetPagedAsync(int pageNumber, int pageSize);
        Task<Patient?> GetPatientWithTreatmentPlanAsync(string patientId);
        //Task<IEnumerable<Patient>> GetAllWithDoctorAndCaregiverAsync();
        //Task<Patient> GetByIdWithDoctorAndCaregiverAsync(string id);
    }
}
