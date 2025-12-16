using BreastCancer.Models;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Context
{
    public class BreastCancerDB : IdentityDbContext<ApplicationUser,ApplicationRole ,string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Patient> Patients { get; set; }
        public virtual DbSet<Doctor> Doctors{ get; set; }
        public virtual DbSet<Caregiver> Caregivers{ get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Doctor>().ToTable("Doctors");
            modelBuilder.Entity<Patient>().ToTable("Patients");
            modelBuilder.Entity<Caregiver>().ToTable("Caregivers");

            ConfigureUserEntity<Doctor>(modelBuilder);
            ConfigureUserEntity<Patient>(modelBuilder);
            ConfigureUserEntity<Caregiver>(modelBuilder);

            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Doctor)
                .WithMany(d => d.Patients)
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.NoAction); 

            modelBuilder.Entity<Caregiver>()
                .HasOne(c => c.Patient)
                .WithMany(p => p.Caregivers)
                .HasForeignKey(c => c.PatientId)
                .OnDelete(DeleteBehavior.NoAction);
        }

        private void ConfigureUserEntity<T>(ModelBuilder modelBuilder) where T : User
        {
            modelBuilder.Entity<T>()
                .Property(u => u.Gender)
                .HasConversion<string>()
                .HasMaxLength(10);
        }
    }
}