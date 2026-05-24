using BreastCancer.Models;

namespace BreastCancer.Repository.Interface
{
    public interface IPatientDiagnosisRepository : IGenericRepository<PatientDiagnosis>
    {
        Task<PatientDiagnosis?> GetByPatientIdAsync(string patientId);
    }
}
