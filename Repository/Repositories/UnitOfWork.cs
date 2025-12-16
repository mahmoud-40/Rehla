using BreastCancer.Context;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;

namespace BreastCancer.Repository.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly BreastCancerDB context;

        private IDoctorRepository _doctorsRepository;
        private IPatientRepository _patientsRepository;
        private ICaregiverRepository _caregiversRepository;
        public UnitOfWork(BreastCancerDB Context)
        {
            this.context = Context;
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
