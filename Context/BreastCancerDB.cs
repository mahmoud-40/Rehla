using BreastCancer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Context
{
    public class BreastCancerDB : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public BreastCancerDB(DbContextOptions options)
            : base(options)
        {
        }

        public virtual DbSet<Patient> Patients { get; set; }
        public virtual DbSet<Doctor> Doctors { get; set; }
        public virtual DbSet<Caregiver> Caregivers { get; set; }
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

        public virtual DbSet<TreatmentPlan> TreatmentPlans { get; set; }
        public virtual DbSet<Medicine> Medicines { get; set; }
        public virtual DbSet<TreatmentPlanHistory> TreatmentPlanHistories { get; set; }
        public virtual DbSet<TreatmentPlanMedia> TreatmentPlanMedias { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Caregiver)
                .WithOne(c => c.User)
                .HasForeignKey<Caregiver>(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Doctor)
                .WithOne(d => d.User)
                .HasForeignKey<Doctor>(d => d.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Patient)
                .WithOne(p => p.User)
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction);

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

            modelBuilder.Entity<Caregiver>()
                .Property(c => c.RelationshipType)
                .HasConversion<string>()
                .HasMaxLength(50);

            modelBuilder.Entity<Patient>()
                .HasKey(p => p.UserId);

            modelBuilder.Entity<Doctor>()
                .HasKey(d => d.UserId);

            modelBuilder.Entity<Caregiver>()
                .HasKey(c => c.UserId);

            modelBuilder.Entity<ApplicationUser>()
               .Property(u => u.Gender)
               .HasConversion<string>()
               .HasMaxLength(10);

            modelBuilder.Entity<Doctor>()
                .HasIndex(d => d.LicenseNumber)
                .IsUnique()
                .HasFilter("[LicenseNumber] IS NOT NULL");


            modelBuilder.Entity<ApplicationUser>()
                .Property(u => u.DateOfBirth)
                .IsRequired(false);

            modelBuilder.Entity<ApplicationRole>().HasData(
                new ApplicationRole { Id = "1", Name = "Admin", NormalizedName = "ADMIN" },
                new ApplicationRole { Id = "2", Name = "Patient", NormalizedName = "PATIENT" },
                new ApplicationRole { Id = "3", Name = "Doctor", NormalizedName = "DOCTOR" },
                new ApplicationRole { Id = "4", Name = "Caregiver", NormalizedName = "CAREGIVER" }
            );

            // TreatmentPlan relationships
            modelBuilder.Entity<TreatmentPlan>()
                .HasOne(tp => tp.Patient)
                .WithMany()
                .HasForeignKey(tp => tp.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TreatmentPlan>()
                .HasOne(tp => tp.Doctor)
                .WithMany()
                .HasForeignKey(tp => tp.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TreatmentPlan>()
                .HasMany(tp => tp.Medicines)
                .WithOne(m => m.TreatmentPlan)
                .HasForeignKey(m => m.TreatmentPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Medicine>()
                .HasOne(m => m.TreatmentPlan)
                .WithMany(tp => tp.Medicines)
                .HasForeignKey(m => m.TreatmentPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
}