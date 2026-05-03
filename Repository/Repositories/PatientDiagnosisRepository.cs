using BreastCancer.Context;
using BreastCancer.DTO.request;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Repository.Repositories
{
    public class PatientDiagnosisRepository : GenericRepository<PatientDiagnosis>, IPatientDiagnosisRepository
    {
        public PatientDiagnosisRepository(BreastCancerDB _Context) : base(_Context)
        {
        }

        public async Task<PatientDiagnosis?> GetByPatientIdAsync(string patientId)
        {
            return await _Context.PatientDiagnoses
                .Include(pd => pd.Patient)
                .FirstOrDefaultAsync(pd => pd.UserId == patientId);
        }
    }
}
