using BreastCancer.Context;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Repository.Repositories
{
    public class CaregiverRepository : GenericRepository<Caregiver> , ICaregiverRepository
    {
        public CaregiverRepository(BreastCancerDB context) : base(context)
        {

        }


    }
}
