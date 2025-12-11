namespace BreastCancer.Repository.Interface
{
    public interface IUnitOfWork
    {
        IUserRepository UsersRepository { get; }
        IDoctorRepository DoctorsRepository { get; }
        IPatientRepository PatientsRepository { get; }
        ICaregiverRepository CaregiversRepository { get; }
    }
}
