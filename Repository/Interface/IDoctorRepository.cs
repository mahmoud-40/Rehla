using BreastCancer.Models;
using System.Runtime.CompilerServices;

namespace BreastCancer.Repository.Interface
{
    public interface IDoctorRepository : IGenericRepository<Doctor>
    {
        //Task<IEnumerable<Doctor>> GetAllWithPatientsAsync();
        //Task<IEnumerable<Doctor>> GetBySpecializationAsync(string specialization);

    }
}
