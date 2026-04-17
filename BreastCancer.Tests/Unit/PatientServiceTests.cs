using AutoMapper;
using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using BreastCancer.Service.Implementation;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BreastCancer.Tests.Unit;

public class PatientServiceTests
{
    [Fact]
    public async Task GetPatientByIdAsync_WhenPatientExists_ReturnsMappedPatient()
    {
        var sut = CreateSut();
        var patient = CreatePatientEntity("patient-1");

        sut.PatientRepository.Setup(x => x.GetByIdAsync("patient-1")).ReturnsAsync(patient);

        var result = await sut.Service.GetPatientByIdAsync("patient-1");

        result.Should().NotBeNull();
        result!.Id.Should().Be("patient-1");
        result.Email.Should().Be("patient@example.com");
        result.MedicalHistory.Should().Be("History");
    }

    [Fact]
    public async Task GetPatientByIdAsync_WhenPatientMissing_ReturnsNull()
    {
        var sut = CreateSut();

        sut.PatientRepository.Setup(x => x.GetByIdAsync("missing")).ReturnsAsync((Patient?)null);

        var result = await sut.Service.GetPatientByIdAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllPatientsAsync_WhenPageIsValid_ReturnsMappedPatients()
    {
        var sut = CreateSut();
        var patients = new[]
        {
            CreatePatientEntity("patient-1"),
            CreatePatientEntity("patient-2", "second@example.com")
        };

        sut.PatientRepository.Setup(x => x.GetPagedAsync(1, 10)).ReturnsAsync(patients);

        var result = await sut.Service.GetAllPatientsAsync(1, 10);

        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().ContainInOrder("patient-1", "patient-2");
    }

    [Fact]
    public async Task GetAllPatientsAsync_WhenPageNumberInvalid_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var act = async () => await sut.Service.GetAllPatientsAsync(0, 10);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Page number must be greater than 0.*");
    }

    [Fact]
    public async Task CreatePatientAsync_WhenEmailAlreadyExists_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        sut.UserManager.Setup(x => x.FindByEmailAsync("patient@example.com"))
            .ReturnsAsync(new ApplicationUser { Id = "existing", Email = "patient@example.com" });

        var act = async () => await sut.Service.CreatePatientAsync(new PatientCreateDTO
        {
            FirstName = "Pat",
            LastName = "Ient",
            Email = "patient@example.com"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A user with email 'patient@example.com' already exists.");
    }

    [Fact]
    public async Task CreatePatientAsync_WhenDoctorIsMissing_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        sut.UserManager.Setup(x => x.FindByEmailAsync("patient@example.com")).ReturnsAsync((ApplicationUser?)null);
        sut.DoctorRepository.Setup(x => x.GetByIdAsync("doctor-1")).ReturnsAsync((Doctor?)null);

        var act = async () => await sut.Service.CreatePatientAsync(new PatientCreateDTO
        {
            FirstName = "Pat",
            LastName = "Ient",
            Email = "patient@example.com",
            DoctorId = "doctor-1"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Doctor with ID 'doctor-1' not found.");
    }

    [Fact]
    public async Task CreatePatientAsync_WhenValid_CreatesAndReturnsPatient()
    {
        var sut = CreateSut();
        sut.UserManager.Setup(x => x.FindByEmailAsync("patient@example.com")).ReturnsAsync((ApplicationUser?)null);
        sut.UserManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Callback<ApplicationUser, string>((user, _) => user.Id = "patient-1")
            .ReturnsAsync(IdentityResult.Success);
        sut.UserManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Patient"))
            .ReturnsAsync(IdentityResult.Success);

        var createdPatient = CreatePatientEntity("patient-1");
        sut.PatientRepository.Setup(x => x.GetByIdAsync("patient-1")).ReturnsAsync(createdPatient);
        sut.PatientRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await sut.Service.CreatePatientAsync(new PatientCreateDTO
        {
            FirstName = "Pat",
            LastName = "Ient",
            Email = "patient@example.com",
            PhoneNumber = "1234567890",
            Address = "Addr",
            MedicalHistory = "History"
        });

        result.Should().NotBeNull();
        result.Id.Should().Be("patient-1");
        result.Email.Should().Be("patient@example.com");
        result.MedicalHistory.Should().Be("History");

        sut.PatientRepository.Verify(x => x.Add(It.IsAny<Patient>()), Times.Once);
        sut.PatientRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        sut.UserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Once);
        sut.UserManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Patient"), Times.Once);
    }

    [Fact]
    public async Task UpdatePatientAsync_WhenPatientMissing_ReturnsNull()
    {
        var sut = CreateSut();
        sut.PatientRepository.Setup(x => x.GetByIdAsync("missing")).ReturnsAsync((Patient?)null);

        var result = await sut.Service.UpdatePatientAsync("missing", new PatientUpdateDTO());

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePatientAsync_WhenValid_UpdatesAndReturnsMappedPatient()
    {
        var sut = CreateSut();
        var patient = CreatePatientEntity("patient-1");
        sut.PatientRepository.SetupSequence(x => x.GetByIdAsync("patient-1"))
            .ReturnsAsync(patient)
            .ReturnsAsync(CreatePatientEntity("patient-1", "updated@example.com", "Updated", "User", "Updated history"));
        sut.UserManager.Setup(x => x.FindByEmailAsync("updated@example.com")).ReturnsAsync((ApplicationUser?)null);
        sut.UserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        sut.PatientRepository.Setup(x => x.Update(It.IsAny<Patient>()));
        sut.PatientRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await sut.Service.UpdatePatientAsync("patient-1", new PatientUpdateDTO
        {
            FirstName = "Updated",
            Email = "updated@example.com",
            MedicalHistory = "Updated history",
            IsActive = false
        });

        result.Should().NotBeNull();
        result!.Id.Should().Be("patient-1");
        result.Email.Should().Be("updated@example.com");
        result.MedicalHistory.Should().Be("Updated history");
        result.FirstName.Should().Be("Updated");
    }

    [Fact]
    public async Task DeletePatientAsync_WhenValid_DeactivatesUser()
    {
        var sut = CreateSut();
        var patient = CreatePatientEntity("patient-1");
        sut.PatientRepository.Setup(x => x.GetByIdAsync("patient-1")).ReturnsAsync(patient);
        sut.UserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        sut.PatientRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        await sut.Service.DeletePatientAsync("patient-1");

        sut.UserManager.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.IsActive == false)), Times.Once);
        sut.PatientRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task HardDeletePatientAsync_WhenValid_DeletesUser()
    {
        var sut = CreateSut();
        var patient = CreatePatientEntity("patient-1");
        sut.PatientRepository.Setup(x => x.GetByIdAsync("patient-1")).ReturnsAsync(patient);
        sut.UserManager.Setup(x => x.DeleteAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        await sut.Service.HardDeletePatientAsync("patient-1");

        sut.UserManager.Verify(x => x.DeleteAsync(It.Is<ApplicationUser>(u => u.Id == "patient-1")), Times.Once);
    }

    private static Patient CreatePatientEntity(string id, string email = "patient@example.com", string firstName = "Pat", string lastName = "Ient", string? history = "History")
    {
        var user = new ApplicationUser
        {
            Id = id,
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = "1234567890",
            Address = "Addr",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        return new Patient
        {
            UserId = id,
            MedicalHistory = history,
            DoctorId = null,
            User = user
        };
    }

    private static SutContext CreateSut()
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        var unitOfWork = new Mock<IUnitOfWork>();
        var patientRepository = new Mock<IPatientRepository>();
        var doctorRepository = new Mock<IDoctorRepository>();
        var logger = new Mock<ILogger<PatientService>>();
        var mapper = new Mock<IMapper>();
        mapper.Setup(x => x.Map<PatientResponseDTO>(It.IsAny<Patient>()))
            .Returns((Patient source) => MapPatient(source));
        mapper.Setup(x => x.Map<IEnumerable<PatientResponseDTO>>(It.IsAny<IEnumerable<Patient>>()))
            .Returns((IEnumerable<Patient> sources) => sources.Select(MapPatient).ToArray());

        unitOfWork.SetupGet(x => x.PatientsRepository).Returns(patientRepository.Object);
        unitOfWork.SetupGet(x => x.DoctorsRepository).Returns(doctorRepository.Object);

        var service = new PatientService(unitOfWork.Object, userManager.Object, logger.Object, mapper.Object);

        return new SutContext(service, userManager, unitOfWork, patientRepository, doctorRepository);
    }

    private sealed record SutContext(
        PatientService Service,
        Mock<UserManager<ApplicationUser>> UserManager,
        Mock<IUnitOfWork> UnitOfWork,
        Mock<IPatientRepository> PatientRepository,
        Mock<IDoctorRepository> DoctorRepository);

    private static PatientResponseDTO MapPatient(Patient source) => new()
    {
        Id = source.UserId,
        FirstName = source.User?.FirstName ?? string.Empty,
        LastName = source.User?.LastName ?? string.Empty,
        FullName = source.User?.FullName ?? string.Empty,
        Email = source.User?.Email ?? string.Empty,
        PhoneNumber = source.User?.PhoneNumber,
        Address = source.User?.Address,
        ImageUrl = source.User?.ImageUrl,
        DateOfBirth = source.User?.DateOfBirth,
        Age = source.User?.Age,
        Gender = source.User?.Gender,
        IsActive = source.User?.IsActive ?? false,
        MedicalHistory = source.MedicalHistory,
        DoctorId = source.DoctorId,
        TreatmentPlanId = source.TreatmentPlan?.Id,
        DoctorName = source.Doctor?.User?.FullName,
        CreatedAt = source.User?.CreatedAt ?? default,
        UpdatedAt = source.User?.UpdatedAt ?? default
    };
}
