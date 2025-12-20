using BreastCancer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Context
{
    public class BreastCancerDB : IdentityDbContext<ApplicationUser,ApplicationRole,string>
    {
        public BreastCancerDB(DbContextOptions options)
            : base(options)
        {
        }

        public virtual DbSet<Patient> Patients { get; set; }
        public virtual DbSet<Doctor> Doctors{ get; set; }
        public virtual DbSet<Caregiver> Caregivers{ get; set; }
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

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

            modelBuilder.Entity<ApplicationRole>().HasData(
                new ApplicationRole { Id = "1", Name = "Admin", NormalizedName = "ADMIN" },
                new ApplicationRole { Id = "2", Name = "Patient", NormalizedName = "PATIENT" },
                new ApplicationRole { Id = "3", Name = "Doctor", NormalizedName = "DOCTOR" },
                new ApplicationRole { Id = "4", Name = "Caregiver", NormalizedName = "CAREGIVER" }
            );
        }

    }
}