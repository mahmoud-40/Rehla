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
        public virtual DbSet<NutritionPlan> NutritionPlans { get; set; }
        public virtual DbSet<NutritionPlanDay> NutritionPlanDays { get; set; }
        public virtual DbSet<NutritionMeal> NutritionMeals { get; set; }
        public virtual DbSet<MealLog> MealLogs { get; set; }
        public virtual DbSet<PatientDiagnosis> PatientDiagnoses { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Caregiver)
                .WithOne(c => c.User)
                .HasForeignKey<Caregiver>(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Doctor)
                .WithOne(d => d.User)
                .HasForeignKey<Doctor>(d => d.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Patient)
                .WithOne(p => p.User)
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Patient>()
                .HasOne(p => p.Doctor)
                .WithMany(d => d.Patients)
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Caregiver>()
                .HasOne(c => c.Patient)
                .WithMany(p => p.Caregivers)
                .HasForeignKey(c => c.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Caregiver>()
                .Property(c => c.RelationshipType)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Entity<Patient>()
                .HasKey(p => p.UserId);

            builder.Entity<Doctor>()
                .HasKey(d => d.UserId);

            builder.Entity<Caregiver>()
                .HasKey(c => c.UserId);

            builder.Entity<ApplicationUser>()
               .Property(u => u.Gender)
               .HasConversion<string>()
               .HasMaxLength(10);

            builder.Entity<Doctor>()
                .HasIndex(d => d.LicenseNumber)
                .IsUnique()
                .HasFilter("[LicenseNumber] IS NOT NULL");


            builder.Entity<ApplicationUser>()
                .Property(u => u.DateOfBirth)
                .IsRequired(false);

            builder.Entity<ApplicationRole>().HasData(
                new ApplicationRole { Id = "1", Name = "Admin", NormalizedName = "ADMIN" },
                new ApplicationRole { Id = "2", Name = "Patient", NormalizedName = "PATIENT" },
                new ApplicationRole { Id = "3", Name = "Doctor", NormalizedName = "DOCTOR" },
                new ApplicationRole { Id = "4", Name = "Caregiver", NormalizedName = "CAREGIVER" }
            );

            // TreatmentPlan relationships
            builder.Entity<TreatmentPlan>()
                .HasOne(tp => tp.Patient)
                .WithOne(p => p.TreatmentPlan)
                .HasForeignKey<TreatmentPlan>(tp => tp.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<TreatmentPlan>()
                .HasOne(tp => tp.Doctor)
                .WithMany()
                .HasForeignKey(tp => tp.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<TreatmentPlan>()
                .HasMany(tp => tp.Medicines)
                .WithOne(m => m.TreatmentPlan)
                .HasForeignKey(m => m.TreatmentPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Medicine>()
                .HasOne(m => m.TreatmentPlan)
                .WithMany(tp => tp.Medicines)
                .HasForeignKey(m => m.TreatmentPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<NutritionPlan>()
                .HasOne(np => np.Patient)
                .WithMany(p => p.NutritionPlans)
                .HasForeignKey(np => np.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<NutritionPlan>()
                .HasOne(np => np.Doctor)
                .WithMany(d => d.NutritionPlans)
                .HasForeignKey(np => np.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<NutritionPlan>()
                .HasMany(np => np.Days)
                .WithOne(day => day.Plan)
                .HasForeignKey(day => day.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<NutritionPlanDay>()
                .HasMany(day => day.Meals)
                .WithOne(meal => meal.Day)
                .HasForeignKey(meal => meal.DayId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<NutritionMeal>()
                .HasMany(meal => meal.MealLogs)
                .WithOne(log => log.Meal)
                .HasForeignKey(log => log.MealId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MealLog>()
                .HasOne(log => log.Patient)
                .WithMany(patient => patient.MealLogs)
                .HasForeignKey(log => log.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<PatientDiagnosis>()
                .HasOne(pd => pd.Patient)
                .WithOne(p => p.Diagnosis)
                .HasForeignKey<PatientDiagnosis>(pd => pd.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.Entity<NutritionMeal>()
                .Property(meal => meal.Protein)
                .HasPrecision(10, 2);

            builder.Entity<NutritionMeal>()
                .Property(meal => meal.Carbs)
                .HasPrecision(10, 2);

            builder.Entity<NutritionMeal>()
                .Property(meal => meal.Fat)
                .HasPrecision(10, 2);
        }

    }
}