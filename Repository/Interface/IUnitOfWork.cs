namespace BreastCancer.Repository.Interface
{
    public interface IUnitOfWork
    {
        IDoctorRepository DoctorsRepository { get; }
        IPatientRepository PatientsRepository { get; }
        ICaregiverRepository CaregiversRepository { get; }
        ITreatmentPlanRepository TreatmentPlansRepository { get; }
        IRefreshTokenRepository RefreshTokenRepository { get; }
        

        Task<int> SaveAsync();

        void Save();
    }
}
