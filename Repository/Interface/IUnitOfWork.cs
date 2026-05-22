using Microsoft.EntityFrameworkCore.Storage;

namespace BreastCancer.Repository.Interface
{
    public interface IUnitOfWork
    {
        IDoctorRepository DoctorsRepository { get; }
        IPatientRepository PatientsRepository { get; }
        ICaregiverRepository CaregiversRepository { get; }
        ITreatmentPlanRepository TreatmentPlansRepository { get; }
        IRefreshTokenRepository RefreshTokenRepository { get; }

        IPatientDiagnosisRepository PatientDiagnosisRepository { get; }
        IPostRepository PostRepository { get; }

        Task<IDbContextTransaction> BeginTransactionAsync();

        Task<int> SaveAsync();

        void Save();
    }
}
