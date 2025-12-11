using BreastCancer.Context;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;

namespace BreastCancer.Repository.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext context;

        private IUserRepository _usersRepository;
        private IDoctorRepository _doctorsRepository;
        private IPatientRepository _patientsRepository;
        private ICaregiverRepository _caregiversRepository;
        public UnitOfWork(ApplicationDbContext Context)
        {
            this.context = Context;
        }

        //public IUserRepository UserRepository =>
        //    _usersRepository ??= new UserRepository(context);
        public IUserRepository UsersRepository
        {
            get
            {
                if (_usersRepository == null)
                {
                    _usersRepository = new UserRepository(context);
                }
                return _usersRepository;
            }
        }
        public IDoctorRepository DoctorsRepository
        {
            get
            {
                if (_doctorsRepository == null)
                {
                    _doctorsRepository = new DoctorRepository(context);
                }
                return _doctorsRepository;
            }
        }
        public IPatientRepository PatientsRepository
        {
            get
            {
                if (_patientsRepository == null)
                {
                    _patientsRepository = new PatientRepository(context);
                }
                return _patientsRepository;
            }
        }
        public ICaregiverRepository CaregiversRepository
        {
            get
            {
                if (_caregiversRepository == null)
                {
                    _caregiversRepository = new CaregiverRepository(context);
                }
                return _caregiversRepository;
            }
        }


        public void Save()
        {
            context.SaveChanges();
        }


        public async Task<int> SaveAsync()
        {
            return await context.SaveChangesAsync();
        }
    }
}
