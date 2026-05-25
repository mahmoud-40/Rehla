using BreastCancer.Context;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Repository.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly BreastCancerDB context;

        private IDoctorRepository _doctorsRepository;
        private IPatientRepository _patientsRepository;
        private ICaregiverRepository _caregiversRepository;
        private ITreatmentPlanRepository _treatmentPlansRepository;
        private IRefreshTokenRepository _refreshTokenRepository;
        private IPatientDiagnosisRepository _patientDiagnosisRepository;
        private IPostRepository _postRepository;
        private IFollowRepository _followRepository;
        private INotificationRepository _notificationsRepository;

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

        public ITreatmentPlanRepository TreatmentPlansRepository
        {
            get
            {
                if (_treatmentPlansRepository == null)
                {
                    _treatmentPlansRepository = new TreatmentPlanRepository(context);
                }
                return _treatmentPlansRepository;
            }
        }

        public IRefreshTokenRepository RefreshTokenRepository
        {
            get
            {
                if (_refreshTokenRepository == null)
                {
                    _refreshTokenRepository = new RefreshTokenRepository(context);
                }
                return _refreshTokenRepository;
            }
        }

        public IPatientDiagnosisRepository PatientDiagnosisRepository
        {
            get
            {
                if (_patientDiagnosisRepository == null)
                {
                    _patientDiagnosisRepository = new PatientDiagnosisRepository(context);
                }
                return _patientDiagnosisRepository;
            }
        }

        public IPostRepository PostRepository
        {
            get
            {
                if (_postRepository == null)
                {
                    _postRepository = new PostRepository(context);
                }
                return _postRepository;
            }
        }

        public IFollowRepository FollowRepository
        {
            get
            {
                if (_followRepository == null)
                {
                    _followRepository = new FollowRepository(context);
                }
                return _followRepository;
            }
        }

        public INotificationRepository NotificationsRepository
        {
            get
            {
                if (_notificationsRepository == null)
                {
                    _notificationsRepository = new NotificationRepository(context);
                }
                return _notificationsRepository;
            }
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync() => await context.Database.BeginTransactionAsync();

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
