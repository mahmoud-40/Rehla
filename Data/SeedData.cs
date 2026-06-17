using BreastCancer.Models;
using BreastCancer.Context;
using BreastCancer.Enum;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Seeding
{
    public static class DataSeeder
    {
        private const string DoctorUserId = "seed-doctor-001";
        private const string Patient1UserId = "seed-patient-001";
        private const string Patient2UserId = "seed-patient-002";
        private const string CaregiverUserId = "seed-caregiver-001";

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BreastCancerDB>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await db.Database.MigrateAsync();

            if (await db.Posts.IgnoreQueryFilters().AnyAsync()) return;

            // ────────────────────────────────────────────────────────────────
            // 1. USERS
            // ────────────────────────────────────────────────────────────────
            var doctorUser = new ApplicationUser
            {
                Id = DoctorUserId,
                UserName = "dr_layla_hassan",
                NormalizedUserName = "DR_LAYLA_HASSAN",
                Email = "layla.hassan@hospital.com",
                NormalizedEmail = "LAYLA.HASSAN@HOSPITAL.COM",
                EmailConfirmed = true,
                FirstName = "Layla",
                LastName = "Hassan",
                Gender = Gender.Female,
                DateOfBirth = new DateTime(1980, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                Address = "Cairo Medical Center, Cairo, Egypt",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UpdatedAt = DateTime.UtcNow.AddMonths(-6),
                PhoneNumber = "+201001234567",
                PhoneNumberConfirmed = true,
            };

            var patient1User = new ApplicationUser
            {
                Id = Patient1UserId,
                UserName = "sara_ahmed",
                NormalizedUserName = "SARA_AHMED",
                Email = "sara.ahmed@gmail.com",
                NormalizedEmail = "SARA.AHMED@GMAIL.COM",
                EmailConfirmed = true,
                FirstName = "Sara",
                LastName = "Ahmed",
                Gender = Gender.Female,
                DateOfBirth = new DateTime(1990, 7, 22, 0, 0, 0, DateTimeKind.Utc),
                Address = "Nasr City, Cairo, Egypt",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-5),
                UpdatedAt = DateTime.UtcNow.AddMonths(-5),
                PhoneNumber = "+201112345678",
                PhoneNumberConfirmed = true,
            };

            var patient2User = new ApplicationUser
            {
                Id = Patient2UserId,
                UserName = "nour_ibrahim",
                NormalizedUserName = "NOUR_IBRAHIM",
                Email = "nour.ibrahim@gmail.com",
                NormalizedEmail = "NOUR.IBRAHIM@GMAIL.COM",
                EmailConfirmed = true,
                FirstName = "Nour",
                LastName = "Ibrahim",
                Gender = Gender.Female,
                DateOfBirth = new DateTime(1985, 11, 8, 0, 0, 0, DateTimeKind.Utc),
                Address = "Maadi, Cairo, Egypt",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                UpdatedAt = DateTime.UtcNow.AddMonths(-3),
                PhoneNumber = "+201223456789",
                PhoneNumberConfirmed = true,
            };

            var caregiverUser = new ApplicationUser
            {
                Id = CaregiverUserId,
                UserName = "khaled_ahmed",
                NormalizedUserName = "KHALED_AHMED",
                Email = "khaled.ahmed@gmail.com",
                NormalizedEmail = "KHALED.AHMED@GMAIL.COM",
                EmailConfirmed = true,
                FirstName = "Khaled",
                LastName = "Ahmed",
                Gender = Gender.Male,
                DateOfBirth = new DateTime(1988, 5, 10, 0, 0, 0, DateTimeKind.Utc),
                Address = "Nasr City, Cairo, Egypt",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-5),
                UpdatedAt = DateTime.UtcNow.AddMonths(-5),
                PhoneNumber = "+201334567890",
                PhoneNumberConfirmed = true,
            };

            await CreateUserAsync(userManager, doctorUser, "Doctor@1234", "Doctor");
            await CreateUserAsync(userManager, patient1User, "Patient@1234", "Patient");
            await CreateUserAsync(userManager, patient2User, "Patient@1234", "Patient");
            await CreateUserAsync(userManager, caregiverUser, "Caregiver@1234", "Caregiver");

            // ────────────────────────────────────────────────────────────────
            // 2. DOCTOR PROFILE
            // ────────────────────────────────────────────────────────────────
            var doctor = new Doctor
            {
                UserId = DoctorUserId,
                Specialization = "Oncology",
                LicenseNumber = "EG-ONC-2018-4521",
                YearsOfExperience = 12,
                NationalIdImage = "https://example.com/images/doctors/layla_id.jpg",
                IsVerified = true,
                User = doctorUser,
            };
            await db.Doctors.AddAsync(doctor);

            // ────────────────────────────────────────────────────────────────
            // 3. PATIENT PROFILES
            // ────────────────────────────────────────────────────────────────
            var patient1 = new Patient
            {
                UserId = Patient1UserId,
                MedicalHistory = "Diagnosed with Stage II breast cancer in 2023. Completed lumpectomy. Currently undergoing chemotherapy.",
                DoctorId = DoctorUserId,
                User = patient1User,
            };

            var patient2 = new Patient
            {
                UserId = Patient2UserId,
                MedicalHistory = "Diagnosed with Stage I breast cancer in 2024. On hormone therapy. No prior surgeries.",
                DoctorId = DoctorUserId,
                User = patient2User,
            };

            await db.Patients.AddRangeAsync(patient1, patient2);

            // ────────────────────────────────────────────────────────────────
            // 4. CAREGIVER PROFILE
            // ────────────────────────────────────────────────────────────────
            var caregiver = new Caregiver
            {
                UserId = CaregiverUserId,
                RelationshipType = RelationshipType.OTHER,
                PatientId = Patient1UserId,
                Patient = patient1,
                User = caregiverUser,
            };
            await db.Caregivers.AddAsync(caregiver);

            await db.SaveChangesAsync();

            // ────────────────────────────────────────────────────────────────
            // 5. PATIENT DIAGNOSES
            // ────────────────────────────────────────────────────────────────
            var diagnoses = new List<PatientDiagnosis>
            {
                new PatientDiagnosis
                {
                    UserId                  = Patient1UserId,
                    AgeAtDiagnosis          = 33,
                    CancerType              = "Breast Cancer",
                    CancerTypeDetailed      = "Invasive Ductal Carcinoma",
                    TumorStage              = "Stage II",
                    NeoplasmHistologicGrade = "Grade 2",
                    ErStatus                = "Positive",
                    PrStatus                = "Positive",
                    Her2Status              = "Negative",
                    Chemotherapy            = true,
                    HormoneTherapy          = true,
                    RadioTherapy            = false,
                    Patient                 = patient1,
                },
                new PatientDiagnosis
                {
                    UserId                  = Patient2UserId,
                    AgeAtDiagnosis          = 39,
                    CancerType              = "Breast Cancer",
                    CancerTypeDetailed      = "Ductal Carcinoma In Situ (DCIS)",
                    TumorStage              = "Stage I",
                    NeoplasmHistologicGrade = "Grade 1",
                    ErStatus                = "Positive",
                    PrStatus                = "Negative",
                    Her2Status              = "Negative",
                    Chemotherapy            = false,
                    HormoneTherapy          = true,
                    RadioTherapy            = true,
                    Patient                 = patient2,
                },
            };
            await db.PatientDiagnoses.AddRangeAsync(diagnoses);
            await db.SaveChangesAsync();

            // ────────────────────────────────────────────────────────────────
            // 6. TREATMENT PLANS  (int PK — save to get generated IDs)
            // ────────────────────────────────────────────────────────────────
            var treatmentPlan1 = new TreatmentPlan
            {
                Name = "Sara – AC-T Chemo Cycle 3",
                Description = "Third cycle of AC-T chemotherapy protocol. Monitor CBC weekly. Pre-medicate with antiemetics.",
                StartDate = DateTime.UtcNow.AddMonths(-2),
                EndDate = DateTime.UtcNow.AddMonths(2),
                Status = TreatmentPlanStatus.InProgress,
                PatientId = Patient1UserId,
                Patient = patient1,
                DoctorId = DoctorUserId,
                Doctor = doctor,
                DoctorName = "Dr. Layla Hassan",
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                CreatedBy = DoctorUserId,
                UpdatedAt = DateTime.UtcNow.AddMonths(-2),
                UpdatedBy = DoctorUserId,
            };

            var treatmentPlan2 = new TreatmentPlan
            {
                Name = "Nour – Tamoxifen Hormone Therapy",
                Description = "Tamoxifen 20mg daily for 5 years. Annual bone density scan required.",
                StartDate = DateTime.UtcNow.AddMonths(-1),
                EndDate = DateTime.UtcNow.AddYears(5),
                Status = TreatmentPlanStatus.InProgress,
                PatientId = Patient2UserId,
                Patient = patient2,
                DoctorId = DoctorUserId,
                Doctor = doctor,
                DoctorName = "Dr. Layla Hassan",
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                CreatedBy = DoctorUserId,
                UpdatedAt = DateTime.UtcNow.AddMonths(-1),
                UpdatedBy = DoctorUserId,
            };

            await db.TreatmentPlans.AddRangeAsync(treatmentPlan1, treatmentPlan2);
            await db.SaveChangesAsync();
            // Now treatmentPlan1.Id and treatmentPlan2.Id are populated

            // ────────────────────────────────────────────────────────────────
            // 7. MEDICINES
            // ────────────────────────────────────────────────────────────────
            var medicines = new List<Medicine>
            {
                new Medicine
                {
                    Name            = "Doxorubicin (Adriamycin)",
                    Instruction     = "Administer IV on Day 1 of each 21-day cycle. Pre-medicate with antiemetics 30 min before.",
                    StartTime       = DateTime.UtcNow.Date.AddHours(9),
                    IntervalHours   = 504, // 21 days
                    EndTime         = DateTime.UtcNow.Date.AddHours(10),
                    LastTaken       = DateTime.UtcNow.AddDays(-21),
                    NextAlert       = DateTime.UtcNow,
                    CreatedAt       = DateTime.UtcNow.AddMonths(-2),
                    CreatedBy       = DoctorUserId,
                    UpdatedAt       = DateTime.UtcNow.AddMonths(-2),
                    UpdatedBy       = DoctorUserId,
                    TreatmentPlanId = treatmentPlan1.Id,
                    TreatmentPlan   = treatmentPlan1,
                },
                new Medicine
                {
                    Name            = "Cyclophosphamide",
                    Instruction     = "Administer IV on Day 1 of each 21-day cycle with Doxorubicin. Drink 2–3L of water daily.",
                    StartTime       = DateTime.UtcNow.Date.AddHours(9).AddMinutes(30),
                    IntervalHours   = 504,
                    EndTime         = DateTime.UtcNow.Date.AddHours(11),
                    LastTaken       = DateTime.UtcNow.AddDays(-21),
                    NextAlert       = DateTime.UtcNow,
                    CreatedAt       = DateTime.UtcNow.AddMonths(-2),
                    CreatedBy       = DoctorUserId,
                    UpdatedAt       = DateTime.UtcNow.AddMonths(-2),
                    UpdatedBy       = DoctorUserId,
                    TreatmentPlanId = treatmentPlan1.Id,
                    TreatmentPlan   = treatmentPlan1,
                },
                new Medicine
                {
                    Name            = "Ondansetron (Zofran)",
                    Instruction     = "Take 8mg orally 30 minutes before chemo and every 8 hours for 2 days after each session.",
                    StartTime       = DateTime.UtcNow.Date.AddHours(8).AddMinutes(30),
                    IntervalHours   = 8,
                    EndTime         = DateTime.UtcNow.Date.AddHours(8).AddMinutes(45),
                    LastTaken       = DateTime.UtcNow.AddHours(-8),
                    NextAlert       = DateTime.UtcNow,
                    CreatedAt       = DateTime.UtcNow.AddMonths(-2),
                    CreatedBy       = DoctorUserId,
                    UpdatedAt       = DateTime.UtcNow.AddMonths(-2),
                    UpdatedBy       = DoctorUserId,
                    TreatmentPlanId = treatmentPlan1.Id,
                    TreatmentPlan   = treatmentPlan1,
                },
                new Medicine
                {
                    Name            = "Tamoxifen",
                    Instruction     = "Take 20mg orally once daily. Always take at the same time each day. Do not stop without consulting doctor.",
                    StartTime       = DateTime.UtcNow.Date.AddHours(8),
                    IntervalHours   = 24,
                    EndTime         = DateTime.UtcNow.Date.AddHours(8).AddMinutes(15),
                    LastTaken       = DateTime.UtcNow.AddHours(-24),
                    NextAlert       = DateTime.UtcNow,
                    CreatedAt       = DateTime.UtcNow.AddMonths(-1),
                    CreatedBy       = DoctorUserId,
                    UpdatedAt       = DateTime.UtcNow.AddMonths(-1),
                    UpdatedBy       = DoctorUserId,
                    TreatmentPlanId = treatmentPlan2.Id,
                    TreatmentPlan   = treatmentPlan2,
                },
                new Medicine
                {
                    Name            = "Calcium + Vitamin D Supplement",
                    Instruction     = "Take 1 tablet twice daily with meals to prevent bone density loss during hormone therapy.",
                    StartTime       = DateTime.UtcNow.Date.AddHours(7),
                    IntervalHours   = 12,
                    EndTime         = DateTime.UtcNow.Date.AddHours(7).AddMinutes(5),
                    LastTaken       = DateTime.UtcNow.AddHours(-12),
                    NextAlert       = DateTime.UtcNow,
                    CreatedAt       = DateTime.UtcNow.AddMonths(-1),
                    CreatedBy       = DoctorUserId,
                    UpdatedAt       = DateTime.UtcNow.AddMonths(-1),
                    UpdatedBy       = DoctorUserId,
                    TreatmentPlanId = treatmentPlan2.Id,
                    TreatmentPlan   = treatmentPlan2,
                },
            };
            await db.Medicines.AddRangeAsync(medicines);

            // ────────────────────────────────────────────────────────────────
            // 8. TREATMENT PLAN HISTORIES  (save to get IDs for media)
            // ────────────────────────────────────────────────────────────────
            var tp1History1 = new TreatmentPlanHistory
            {
                TreatmentPlanId = treatmentPlan1.Id,
                TreatmentPlan = treatmentPlan1,
                Status = TreatmentPlanStatus.NotStarted,
                ChangedAt = DateTime.UtcNow.AddMonths(-2).AddDays(-1),
                ChangedBy = DoctorUserId,
            };
            var tp1History2 = new TreatmentPlanHistory
            {
                TreatmentPlanId = treatmentPlan1.Id,
                TreatmentPlan = treatmentPlan1,
                Status = TreatmentPlanStatus.InProgress,
                ChangedAt = DateTime.UtcNow.AddMonths(-2),
                ChangedBy = DoctorUserId,
            };
            var tp2History1 = new TreatmentPlanHistory
            {
                TreatmentPlanId = treatmentPlan2.Id,
                TreatmentPlan = treatmentPlan2,
                Status = TreatmentPlanStatus.InProgress,
                ChangedAt = DateTime.UtcNow.AddMonths(-1),
                ChangedBy = DoctorUserId,
            };

            await db.TreatmentPlanHistories.AddRangeAsync(tp1History1, tp1History2, tp2History1);
            await db.SaveChangesAsync();
            // Now history IDs are populated

            // ────────────────────────────────────────────────────────────────
            // 9. TREATMENT PLAN MEDIA
            // ────────────────────────────────────────────────────────────────
            var medias = new List<TreatmentPlanMedia>
            {
                new TreatmentPlanMedia
                {
                    MediaUrl               = "https://example.com/media/sara_scan_report.pdf",
                    MediaType              = "application/pdf",
                    TreatmentPlanHistoryId = tp1History2.Id,
                    TreatmentPlanHistory   = tp1History2,
                    CreatedAt              = DateTime.UtcNow.AddMonths(-2),
                    CreatedBy              = DoctorUserId,
                    UpdatedAt              = DateTime.UtcNow.AddMonths(-2),
                    UpdatedBy              = DoctorUserId,
                },
                new TreatmentPlanMedia
                {
                    MediaUrl               = "https://example.com/media/sara_xray.jpg",
                    MediaType              = "image/jpeg",
                    TreatmentPlanHistoryId = tp1History2.Id,
                    TreatmentPlanHistory   = tp1History2,
                    CreatedAt              = DateTime.UtcNow.AddMonths(-2),
                    CreatedBy              = DoctorUserId,
                    UpdatedAt              = DateTime.UtcNow.AddMonths(-2),
                    UpdatedBy              = DoctorUserId,
                },
            };
            await db.TreatmentPlanMedias.AddRangeAsync(medias);

            // ────────────────────────────────────────────────────────────────
            // 10. NUTRITION PLANS  (save to get int IDs)
            // ────────────────────────────────────────────────────────────────
            var nutritionPlan1 = new NutritionPlan
            {
                PatientId = Patient1UserId,
                Patient = patient1,
                CreatedBy = DoctorUserId,
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                Status = NutritionPlanStatus.Approved,
                Source = NutritionPlanSource.Manual,
                IsLocked = false,
                DoctorId = DoctorUserId,
                Doctor = doctor,
                ApprovedAt = DateTime.UtcNow.AddMonths(-2).AddDays(1),
                ApprovedBy = DoctorUserId,
                RejectionNote = null,
            };

            var nutritionPlan2 = new NutritionPlan
            {
                PatientId = Patient2UserId,
                Patient = patient2,
                CreatedBy = DoctorUserId,
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                Status = NutritionPlanStatus.Approved,
                Source = NutritionPlanSource.Manual,
                IsLocked = false,
                DoctorId = DoctorUserId,
                Doctor = doctor,
                ApprovedAt = DateTime.UtcNow.AddMonths(-1).AddDays(1),
                ApprovedBy = DoctorUserId,
                RejectionNote = null,
            };

            await db.NutritionPlans.AddRangeAsync(nutritionPlan1, nutritionPlan2);
            await db.SaveChangesAsync();

            // ────────────────────────────────────────────────────────────────
            // 11. NUTRITION PLAN DAYS  (save to get IDs for meals)
            // ────────────────────────────────────────────────────────────────
            var np1Days = Enumerable.Range(1, 7).Select(d => new NutritionPlanDay
            {
                PlanId = nutritionPlan1.Id,
                Plan = nutritionPlan1,
                DayNumber = d,
            }).ToList();

            var np2Days = Enumerable.Range(1, 7).Select(d => new NutritionPlanDay
            {
                PlanId = nutritionPlan2.Id,
                Plan = nutritionPlan2,
                DayNumber = d,
            }).ToList();

            await db.NutritionPlanDays.AddRangeAsync(np1Days);
            await db.NutritionPlanDays.AddRangeAsync(np2Days);
            await db.SaveChangesAsync();

            // ────────────────────────────────────────────────────────────────
            // 12. NUTRITION MEALS  (save to get IDs for meal logs)
            // ────────────────────────────────────────────────────────────────
            var mealsDay1Plan1 = new List<NutritionMeal>
            {
                new NutritionMeal
                {
                    DayId        = np1Days[0].Id,
                    Day          = np1Days[0],
                    MealType     = MealType.Breakfast,
                    Name         = "Oatmeal with Berries & Flaxseed",
                    Calories     = 380,
                    Protein      = 12.5m,
                    Carbs        = 58.0m,
                    Fat          = 9.0m,
                    Benefits     = "High in antioxidants and fiber. Supports immune system during chemo.",
                    Instructions = "Cook oats in low-fat milk, top with fresh blueberries, strawberries, and 1 tbsp ground flaxseed.",
                    Notes        = "Avoid adding sugar. Use honey sparingly if needed.",
                },
                new NutritionMeal
                {
                    DayId        = np1Days[0].Id,
                    Day          = np1Days[0],
                    MealType     = MealType.Lunch,
                    Name         = "Grilled Salmon with Steamed Broccoli & Quinoa",
                    Calories     = 520,
                    Protein      = 42.0m,
                    Carbs        = 38.0m,
                    Fat          = 18.0m,
                    Benefits     = "Omega-3 rich, anti-inflammatory. Supports cell repair post-chemo.",
                    Instructions = "Grill salmon with lemon and herbs. Steam broccoli 5 min. Cook quinoa per package instructions.",
                    Notes        = "If nausea is present, reduce portion size and eat slowly.",
                },
                new NutritionMeal
                {
                    DayId        = np1Days[0].Id,
                    Day          = np1Days[0],
                    MealType     = MealType.Dinner,
                    Name         = "Red Lentil Soup with Whole Wheat Bread",
                    Calories     = 420,
                    Protein      = 22.0m,
                    Carbs        = 62.0m,
                    Fat          = 7.0m,
                    Benefits     = "High fiber. Supports gut health during treatment. Easy to digest.",
                    Instructions = "Cook red lentils with cumin, turmeric, carrots, and tomatoes. Serve with 1 slice whole wheat bread.",
                    Notes        = "Ideal for post-chemo days when appetite is low.",
                },
                new NutritionMeal
                {
                    DayId        = np1Days[0].Id,
                    Day          = np1Days[0],
                    MealType     = MealType.Snack,
                    Name         = "Greek Yogurt with Walnuts",
                    Calories     = 200,
                    Protein      = 14.0m,
                    Carbs        = 12.0m,
                    Fat          = 10.0m,
                    Benefits     = "Probiotics support gut microbiome. Walnuts are rich in Omega-3.",
                    Instructions = "Mix plain Greek yogurt with a handful of walnuts and a drizzle of honey.",
                    Notes        = "Best taken mid-morning, about 2–3 hours after breakfast.",
                },
            };

            var mealsDay1Plan2 = new List<NutritionMeal>
            {
                new NutritionMeal
                {
                    DayId        = np2Days[0].Id,
                    Day          = np2Days[0],
                    MealType     = MealType.Breakfast,
                    Name         = "Avocado Toast with Poached Egg",
                    Calories     = 360,
                    Protein      = 18.0m,
                    Carbs        = 32.0m,
                    Fat          = 16.0m,
                    Benefits     = "Healthy fats support hormonal balance. Eggs provide complete protein.",
                    Instructions = "Toast whole wheat bread, spread mashed avocado, top with 1 poached egg and chili flakes.",
                    Notes        = "Avoid soy-based spreads due to potential hormone interaction.",
                },
                new NutritionMeal
                {
                    DayId        = np2Days[0].Id,
                    Day          = np2Days[0],
                    MealType     = MealType.Lunch,
                    Name         = "Chicken & Vegetable Stir Fry with Brown Rice",
                    Calories     = 490,
                    Protein      = 38.0m,
                    Carbs        = 48.0m,
                    Fat          = 12.0m,
                    Benefits     = "Lean protein aids tissue recovery. Low GI carbs maintain stable energy.",
                    Instructions = "Stir fry chicken breast with bell peppers, zucchini, and ginger in olive oil. Serve over brown rice.",
                    Notes        = "Limit sodium. Use low-sodium soy sauce or substitute with herbs.",
                },
                new NutritionMeal
                {
                    DayId        = np2Days[0].Id,
                    Day          = np2Days[0],
                    MealType     = MealType.Dinner,
                    Name         = "Baked Tilapia with Sweet Potato & Spinach Salad",
                    Calories     = 430,
                    Protein      = 34.0m,
                    Carbs        = 40.0m,
                    Fat          = 9.0m,
                    Benefits     = "Low-calorie, high nutrient density. Spinach is iron-rich. Great for weight maintenance.",
                    Instructions = "Season tilapia with garlic and lemon, bake 20 min at 200°C. Roast sweet potato wedges. Toss spinach with olive oil and lemon.",
                    Notes        = "Ideal for maintaining healthy weight during hormone therapy.",
                },
                new NutritionMeal
                {
                    DayId        = np2Days[0].Id,
                    Day          = np2Days[0],
                    MealType     = MealType.Snack,
                    Name         = "Apple Slices with Almond Butter",
                    Calories     = 180,
                    Protein      = 5.0m,
                    Carbs        = 24.0m,
                    Fat          = 8.0m,
                    Benefits     = "Natural sugars for energy. Healthy fats help with satiety.",
                    Instructions = "Slice one medium apple and serve with 1 tbsp almond butter.",
                    Notes        = "Good afternoon snack to maintain energy before dinner.",
                },
            };

            await db.NutritionMeals.AddRangeAsync(mealsDay1Plan1);
            await db.NutritionMeals.AddRangeAsync(mealsDay1Plan2);
            await db.SaveChangesAsync();

            // ────────────────────────────────────────────────────────────────
            // 13. MEAL LOGS
            // ────────────────────────────────────────────────────────────────
            var mealLogs = new List<MealLog>
            {
                new MealLog { MealId = mealsDay1Plan1[0].Id, Meal = mealsDay1Plan1[0], PatientId = Patient1UserId, Patient = patient1, EatenAt = DateTime.UtcNow.AddDays(-3).AddHours(8) },
                new MealLog { MealId = mealsDay1Plan1[1].Id, Meal = mealsDay1Plan1[1], PatientId = Patient1UserId, Patient = patient1, EatenAt = DateTime.UtcNow.AddDays(-3).AddHours(13) },
                new MealLog { MealId = mealsDay1Plan1[2].Id, Meal = mealsDay1Plan1[2], PatientId = Patient1UserId, Patient = patient1, EatenAt = DateTime.UtcNow.AddDays(-3).AddHours(19) },
                new MealLog { MealId = mealsDay1Plan1[3].Id, Meal = mealsDay1Plan1[3], PatientId = Patient1UserId, Patient = patient1, EatenAt = DateTime.UtcNow.AddDays(-3).AddHours(11) },
                new MealLog { MealId = mealsDay1Plan1[0].Id, Meal = mealsDay1Plan1[0], PatientId = Patient1UserId, Patient = patient1, EatenAt = DateTime.UtcNow.AddDays(-2).AddHours(8) },
                new MealLog { MealId = mealsDay1Plan1[1].Id, Meal = mealsDay1Plan1[1], PatientId = Patient1UserId, Patient = patient1, EatenAt = DateTime.UtcNow.AddDays(-2).AddHours(13) },
                new MealLog { MealId = mealsDay1Plan2[0].Id, Meal = mealsDay1Plan2[0], PatientId = Patient2UserId, Patient = patient2, EatenAt = DateTime.UtcNow.AddDays(-2).AddHours(8) },
                new MealLog { MealId = mealsDay1Plan2[1].Id, Meal = mealsDay1Plan2[1], PatientId = Patient2UserId, Patient = patient2, EatenAt = DateTime.UtcNow.AddDays(-2).AddHours(13) },
                new MealLog { MealId = mealsDay1Plan2[2].Id, Meal = mealsDay1Plan2[2], PatientId = Patient2UserId, Patient = patient2, EatenAt = DateTime.UtcNow.AddDays(-2).AddHours(19) },
            };
            await db.MealLogs.AddRangeAsync(mealLogs);

            // ────────────────────────────────────────────────────────────────
            // 14. COMMUNITY – FOLLOWS
            // ────────────────────────────────────────────────────────────────
            var follows = new List<Follow>
            {
                new Follow { FollowerId = Patient1UserId,  FollowingId = DoctorUserId,    CreatedAt = DateTime.UtcNow.AddMonths(-4) },
                new Follow { FollowerId = Patient2UserId,  FollowingId = DoctorUserId,    CreatedAt = DateTime.UtcNow.AddMonths(-2) },
                new Follow { FollowerId = Patient1UserId,  FollowingId = Patient2UserId,  CreatedAt = DateTime.UtcNow.AddMonths(-2) },
                new Follow { FollowerId = Patient2UserId,  FollowingId = Patient1UserId,  CreatedAt = DateTime.UtcNow.AddMonths(-1) },
                new Follow { FollowerId = CaregiverUserId, FollowingId = DoctorUserId,    CreatedAt = DateTime.UtcNow.AddMonths(-4) },
                new Follow { FollowerId = CaregiverUserId, FollowingId = Patient1UserId,  CreatedAt = DateTime.UtcNow.AddMonths(-5) },
            };
            await db.Follows.AddRangeAsync(follows);

            // ────────────────────────────────────────────────────────────────
            // 15. COMMUNITY – POSTS  (save to get int IDs)
            // ────────────────────────────────────────────────────────────────
            var post1 = new Post
            {
                AuthorId = Patient1UserId,
                Author = patient1User,
                Content = "Just finished my 3rd chemo session! 💪 It's hard but I remind myself why I'm fighting. " +
                             "Grateful for everyone's support here. This community keeps me going! 🌸 #BreastCancerWarrior",
                Type = PostType.Story,
                Visibility = PostVisibility.Public,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10),
                IsDeleted = false,
                IsEdited = false,
                MediaUrls = new List<string>(),
            };
            var post2 = new Post
            {
                AuthorId = DoctorUserId,
                Author = doctorUser,
                Content = "🩺 Medical Tip: Early detection of breast cancer dramatically increases survival rates. " +
                             "Women aged 40+ should get annual mammograms. Don't delay your screening! " +
                             "Ask your doctor about the right schedule for you. #Awareness #EarlyDetection",
                Type = PostType.Resource,
                Visibility = PostVisibility.Public,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-7),
                IsDeleted = false,
                IsEdited = false,
                MediaUrls = new List<string> { "https://example.com/media/mammogram-infographic.jpg" },
            };
            var post3 = new Post
            {
                AuthorId = Patient2UserId,
                Author = patient2User,
                Content = "Has anyone here experienced joint pain while on Tamoxifen? " +
                             "I've been on it for 3 months and my knees are really bothering me. " +
                             "Would love to hear your experiences and what helped you cope! 🙏",
                Type = PostType.Question,
                Visibility = PostVisibility.Public,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
                IsDeleted = false,
                IsEdited = false,
                MediaUrls = new List<string>(),
            };
            var post4 = new Post
            {
                AuthorId = Patient1UserId,
                Author = patient1User,
                Content = "Sharing a small win today 🎉 My latest blood work came back better than expected! " +
                             "Dr. Layla says my body is responding well to the treatment. " +
                             "Never give up hope — progress is possible! 💕 #MilestoneAlert",
                Type = PostType.MilestoneShare,
                Visibility = PostVisibility.Public,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-3),
                IsDeleted = false,
                IsEdited = false,
                MediaUrls = new List<string>(),
            };
            var post5 = new Post
            {
                AuthorId = DoctorUserId,
                Author = doctorUser,
                Content = "🔬 Update for my patients: personalized hormone therapy significantly reduces recurrence risk " +
                             "in ER+ breast cancer patients based on the latest clinical evidence. " +
                             "We will discuss what this means for each of you individually in your next appointment. 💊",
                Type = PostType.DoctorUpdate,
                Visibility = PostVisibility.Public,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                IsDeleted = false,
                IsEdited = false,
                MediaUrls = new List<string> { "https://example.com/media/research-summary.pdf" },
            };

            await db.Posts.AddRangeAsync(post1, post2, post3, post4, post5);
            await db.SaveChangesAsync();
            // Now post1.Id … post5.Id are populated

            // ────────────────────────────────────────────────────────────────
            // 16. COMMUNITY – COMMENTS
            // ────────────────────────────────────────────────────────────────
            var comments = new List<Comment>
            {
                // Post 1 – Sara's chemo story
                new Comment { PostId = post1.Id, Post = post1, AuthorId = DoctorUserId,    Author = doctorUser,    Content = "Sara you are incredibly brave! Keep going, we're all rooting for you! 🌸",                  CreatedAt = DateTime.UtcNow.AddDays(-9).AddHours(-2), IsDeleted = false },
                new Comment { PostId = post1.Id, Post = post1, AuthorId = Patient2UserId,  Author = patient2User,  Content = "I felt the same after my 3rd session. It gets easier, I promise! 💕",                    CreatedAt = DateTime.UtcNow.AddDays(-9),              IsDeleted = false },
                new Comment { PostId = post1.Id, Post = post1, AuthorId = CaregiverUserId, Author = caregiverUser, Content = "We are so proud of you Sara! You are stronger than you know 💪",                          CreatedAt = DateTime.UtcNow.AddDays(-8),              IsDeleted = false },

                // Post 2 – Doctor's resource post
                new Comment { PostId = post2.Id, Post = post2, AuthorId = Patient1UserId,  Author = patient1User,  Content = "Thank you Dr. Layla! I almost skipped my mammogram last year. This reminder is so important.", CreatedAt = DateTime.UtcNow.AddDays(-6),           IsDeleted = false },
                new Comment { PostId = post2.Id, Post = post2, AuthorId = Patient2UserId,  Author = patient2User,  Content = "Sharing this with my friends and family right now! Everyone needs to see this. ❤️",        CreatedAt = DateTime.UtcNow.AddDays(-6),              IsDeleted = false },

                // Post 3 – Nour's question about Tamoxifen
                new Comment { PostId = post3.Id, Post = post3, AuthorId = DoctorUserId,    Author = doctorUser,    Content = "Joint pain is a known side effect of Tamoxifen. Low-impact exercise like swimming and yoga helps significantly. Let's discuss in your next appointment.",  CreatedAt = DateTime.UtcNow.AddDays(-4).AddHours(-10), IsDeleted = false },
                new Comment { PostId = post3.Id, Post = post3, AuthorId = Patient1UserId,  Author = patient1User,  Content = "Yes I had the same issue! Warm baths in the evening helped me a lot. Also ask about Vitamin D supplements.",                                              CreatedAt = DateTime.UtcNow.AddDays(-4),               IsDeleted = false },

                // Post 4 – Sara's milestone
                new Comment { PostId = post4.Id, Post = post4, AuthorId = DoctorUserId,    Author = doctorUser,    Content = "So happy for you Sara! Your resilience and commitment to the plan are making the difference. Keep it up! 🌟", CreatedAt = DateTime.UtcNow.AddDays(-2),  IsDeleted = false },
                new Comment { PostId = post4.Id, Post = post4, AuthorId = Patient2UserId,  Author = patient2User,  Content = "This made my day! You are such an inspiration to all of us here 💕",                              CreatedAt = DateTime.UtcNow.AddDays(-2),              IsDeleted = false },
                new Comment { PostId = post4.Id, Post = post4, AuthorId = CaregiverUserId, Author = caregiverUser, Content = "The whole family is celebrating with you Sara! So proud! 🎉",                                       CreatedAt = DateTime.UtcNow.AddDays(-1),              IsDeleted = false },

                // Post 5 – Doctor's update
                new Comment { PostId = post5.Id, Post = post5, AuthorId = Patient2UserId,  Author = patient2User,  Content = "This is so encouraging! Looking forward to discussing it at my next visit. Thank you Dr. Layla! 🙏", CreatedAt = DateTime.UtcNow.AddHours(-18), IsDeleted = false },
                new Comment { PostId = post5.Id, Post = post5, AuthorId = Patient1UserId,  Author = patient1User,  Content = "Thank you for always keeping us informed with the latest research! We are lucky to have you 🌟",     CreatedAt = DateTime.UtcNow.AddHours(-12), IsDeleted = false },
            };
            await db.Comments.AddRangeAsync(comments);

            // ────────────────────────────────────────────────────────────────
            // 17. COMMUNITY – REACTIONS
            //     ReactionType: Like | Support | Insightful
            //     Unique index on (PostId, UserId) — one reaction per user per post
            // ────────────────────────────────────────────────────────────────
            var reactions = new List<Reaction>
            {
                // Post 1
                new Reaction { PostId = post1.Id, Post = post1, UserId = DoctorUserId,    User = doctorUser,    Type = ReactionType.Support    },
                new Reaction { PostId = post1.Id, Post = post1, UserId = Patient2UserId,  User = patient2User,  Type = ReactionType.Support    },
                new Reaction { PostId = post1.Id, Post = post1, UserId = CaregiverUserId, User = caregiverUser, Type = ReactionType.Support    },
                // Post 2
                new Reaction { PostId = post2.Id, Post = post2, UserId = Patient1UserId,  User = patient1User,  Type = ReactionType.Insightful },
                new Reaction { PostId = post2.Id, Post = post2, UserId = Patient2UserId,  User = patient2User,  Type = ReactionType.Insightful },
                new Reaction { PostId = post2.Id, Post = post2, UserId = CaregiverUserId, User = caregiverUser, Type = ReactionType.Like       },
                // Post 3
                new Reaction { PostId = post3.Id, Post = post3, UserId = Patient1UserId,  User = patient1User,  Type = ReactionType.Support    },
                new Reaction { PostId = post3.Id, Post = post3, UserId = DoctorUserId,    User = doctorUser,    Type = ReactionType.Like       },
                // Post 4
                new Reaction { PostId = post4.Id, Post = post4, UserId = Patient2UserId,  User = patient2User,  Type = ReactionType.Support    },
                new Reaction { PostId = post4.Id, Post = post4, UserId = DoctorUserId,    User = doctorUser,    Type = ReactionType.Like       },
                new Reaction { PostId = post4.Id, Post = post4, UserId = CaregiverUserId, User = caregiverUser, Type = ReactionType.Support    },
                // Post 5
                new Reaction { PostId = post5.Id, Post = post5, UserId = Patient1UserId,  User = patient1User,  Type = ReactionType.Insightful },
                new Reaction { PostId = post5.Id, Post = post5, UserId = Patient2UserId,  User = patient2User,  Type = ReactionType.Insightful },
                new Reaction { PostId = post5.Id, Post = post5, UserId = CaregiverUserId, User = caregiverUser, Type = ReactionType.Like       },
            };
            await db.Reactions.AddRangeAsync(reactions);

            // ────────────────────────────────────────────────────────────────
            // 18. NOTIFICATIONS
            //     NotificationType: General|PostCreated|Comment|Follow|Reaction|TreatmentPlan
            // ────────────────────────────────────────────────────────────────
            var notifications = new List<Notification>
            {
                // Sara receives notifications for her posts
                new Notification { UserId = Patient1UserId, User = patient1User, Title = "New Comment",        Message = "Dr. Layla Hassan commented on your post.",                  Type = NotificationType.Comment,      TargetId = post1.Id.ToString(), IsRead = false, CreatedAt = DateTime.UtcNow.AddDays(-9) },
                new Notification { UserId = Patient1UserId, User = patient1User, Title = "New Reaction",       Message = "Someone reacted to your story post.",                        Type = NotificationType.Reaction,     TargetId = post1.Id.ToString(), IsRead = true,  CreatedAt = DateTime.UtcNow.AddDays(-9) },
                new Notification { UserId = Patient1UserId, User = patient1User, Title = "New Follower",       Message = "Nour Ibrahim started following you.",                         Type = NotificationType.Follow,       TargetId = Patient2UserId,      IsRead = true,  CreatedAt = DateTime.UtcNow.AddMonths(-2) },
                new Notification { UserId = Patient1UserId, User = patient1User, Title = "Treatment Updated",  Message = "Your treatment plan has been updated by Dr. Layla Hassan.",  Type = NotificationType.TreatmentPlan,TargetId = treatmentPlan1.Id.ToString(), IsRead = false, CreatedAt = DateTime.UtcNow.AddMonths(-2) },
                new Notification { UserId = Patient1UserId, User = patient1User, Title = "Comment on Post",    Message = "Nour Ibrahim commented on your milestone post.",              Type = NotificationType.Comment,      TargetId = post4.Id.ToString(), IsRead = false, CreatedAt = DateTime.UtcNow.AddDays(-2) },

                // Nour receives notifications
                new Notification { UserId = Patient2UserId, User = patient2User, Title = "Doctor Replied",     Message = "Dr. Layla Hassan answered your question about Tamoxifen.",   Type = NotificationType.Comment,      TargetId = post3.Id.ToString(), IsRead = false, CreatedAt = DateTime.UtcNow.AddDays(-4) },
                new Notification { UserId = Patient2UserId, User = patient2User, Title = "New Follower",       Message = "Sara Ahmed started following you.",                           Type = NotificationType.Follow,       TargetId = Patient1UserId,      IsRead = true,  CreatedAt = DateTime.UtcNow.AddMonths(-1) },
                new Notification { UserId = Patient2UserId, User = patient2User, Title = "New Doctor Update",  Message = "Dr. Layla Hassan posted a new update for patients.",          Type = NotificationType.PostCreated,  TargetId = post5.Id.ToString(), IsRead = false, CreatedAt = DateTime.UtcNow.AddDays(-1) },

                // Doctor receives notifications
                new Notification { UserId = DoctorUserId, User = doctorUser, Title = "Comment on Your Post",   Message = "Sara Ahmed commented on your doctor update post.",            Type = NotificationType.Comment,      TargetId = post5.Id.ToString(), IsRead = false, CreatedAt = DateTime.UtcNow.AddHours(-12) },
                new Notification { UserId = DoctorUserId, User = doctorUser, Title = "New Reaction",           Message = "Patients reacted to your resource post.",                    Type = NotificationType.Reaction,     TargetId = post2.Id.ToString(), IsRead = false, CreatedAt = DateTime.UtcNow.AddHours(-20) },

                // Caregiver receives notifications
                new Notification { UserId = CaregiverUserId, User = caregiverUser, Title = "New Post",         Message = "Sara Ahmed shared a new milestone post.",                    Type = NotificationType.PostCreated,  TargetId = post4.Id.ToString(), IsRead = false, CreatedAt = DateTime.UtcNow.AddDays(-3) },
                new Notification { UserId = CaregiverUserId, User = caregiverUser, Title = "General",          Message = "Welcome to the BreastCancer support community!",             Type = NotificationType.General,      TargetId = null,                IsRead = true,  CreatedAt = DateTime.UtcNow.AddMonths(-5) },
            };
            await db.Notifications.AddRangeAsync(notifications);

            // ────────────────────────────────────────────────────────────────
            // 19. HIGH FOLLOWER POSTS
            // ────────────────────────────────────────────────────────────────
            var highFollowerPosts = new List<HighFollowerPost>
            {
                new HighFollowerPost { PostId = post2.Id, AuthorId = DoctorUserId, CreatedAt = DateTimeOffset.UtcNow.AddDays(-7) },
                new HighFollowerPost { PostId = post5.Id, AuthorId = DoctorUserId, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) },
            };
            await db.HighFollowerPosts.AddRangeAsync(highFollowerPosts);

            // ────────────────────────────────────────────────────────────────
            // 20. REFRESH TOKENS
            // ────────────────────────────────────────────────────────────────
            var refreshTokens = new List<RefreshToken>
            {
                new RefreshToken
                {
                    Token     = "dev-refresh-doctor-layla-001",
                    UserId    = DoctorUserId,
                    User      = doctorUser,
                    ExpiresAt = DateTime.UtcNow.AddDays(30),
                    CreatedAt = DateTime.UtcNow,
                    IsRevoked = false,
                },
                new RefreshToken
                {
                    Token     = "dev-refresh-patient-sara-001",
                    UserId    = Patient1UserId,
                    User      = patient1User,
                    ExpiresAt = DateTime.UtcNow.AddDays(30),
                    CreatedAt = DateTime.UtcNow,
                    IsRevoked = false,
                },
                new RefreshToken
                {
                    Token     = "dev-refresh-patient-nour-001",
                    UserId    = Patient2UserId,
                    User      = patient2User,
                    ExpiresAt = DateTime.UtcNow.AddDays(30),
                    CreatedAt = DateTime.UtcNow,
                    IsRevoked = false,
                },
                new RefreshToken
                {
                    Token     = "dev-refresh-caregiver-khaled-001",
                    UserId    = CaregiverUserId,
                    User      = caregiverUser,
                    ExpiresAt = DateTime.UtcNow.AddDays(30),
                    CreatedAt = DateTime.UtcNow,
                    IsRevoked = false,
                },
            };
            await db.RefreshTokens.AddRangeAsync(refreshTokens);

            await db.SaveChangesAsync();

            Console.WriteLine("✅  Seed data applied successfully.");
            Console.WriteLine($"    Users      : 4  (1 Doctor, 2 Patients, 1 Caregiver)");
            Console.WriteLine($"    Posts      : 5  | Comments: 12 | Reactions: 14 | Follows: 6");
            Console.WriteLine($"    Treatments : 2  | Medicines: 5 | Histories: 3 | Media: 2");
            Console.WriteLine($"    Nutrition  : 2 plans | 14 days | 8 meals | 9 logs");
            Console.WriteLine($"    Notifications: 12 | HighFollowerPosts: 2 | RefreshTokens: 4");
        }

        // ── Helper ──────────────────────────────────────────────────────────
        private static async Task CreateUserAsync(
            UserManager<ApplicationUser> userManager,
            ApplicationUser user,
            string password,
            string role)
        {
            if (await userManager.FindByIdAsync(user.Id) is not null) return;

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, role);
            else
                throw new Exception(
                    $"Failed to seed user '{user.UserName}': " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}