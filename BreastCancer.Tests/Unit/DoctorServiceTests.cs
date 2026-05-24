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

public class DoctorServiceTests
{
    [Fact]
    public async Task GetDoctorByIdAsync_WhenDoctorExists_ReturnsMappedDoctor()
    {
        var sut = CreateSut();
        var doctor = CreateDoctorEntity("doctor-1");

        sut.DoctorRepository.Setup(x => x.GetByIdAsync("doctor-1")).ReturnsAsync(doctor);

        var result = await sut.Service.GetDoctorByIdAsync("doctor-1");

        result.Should().NotBeNull();
        result!.Id.Should().Be("doctor-1");
        result.Email.Should().Be("doctor@example.com");
        result.Specialization.Should().Be("Oncology");
    }

    [Fact]
    public async Task GetDoctorByIdAsync_WhenDoctorMissing_ReturnsNull()
    {
        var sut = CreateSut();

        sut.DoctorRepository.Setup(x => x.GetByIdAsync("missing")).ReturnsAsync((Doctor?)null);

        var result = await sut.Service.GetDoctorByIdAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllDoctorsAsync_WhenPageIsValid_ReturnsMappedDoctors()
    {
        var sut = CreateSut();
        var doctors = new[]
        {
            CreateDoctorEntity("doctor-1"),
            CreateDoctorEntity("doctor-2", "second@example.com")
        };

        sut.DoctorRepository.Setup(x => x.GetPagedAsync(1, 10)).ReturnsAsync(doctors);

        var result = await sut.Service.GetAllDoctorsAsync(1, 10);

        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().ContainInOrder("doctor-1", "doctor-2");
    }

    [Fact]
    public async Task GetAllDoctorsAsync_WhenPageNumberInvalid_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var act = async () => await sut.Service.GetAllDoctorsAsync(0, 10);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Page number must be greater than 0.*");
    }

    [Fact]
    public async Task CreateDoctorAsync_WhenEmailAlreadyExists_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        sut.UserManager.Setup(x => x.FindByEmailAsync("doctor@example.com"))
            .ReturnsAsync(new ApplicationUser { Id = "existing", Email = "doctor@example.com" });

        var act = async () => await sut.Service.CreateDoctorAsync(new DoctorCreateDTO
        {
            FirstName = "Doc",
            LastName = "Tor",
            Email = "doctor@example.com"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A user with email 'doctor@example.com' already exists.");
    }

    [Fact]
    public async Task CreateDoctorAsync_WhenValid_CreatesAndReturnsDoctor()
    {
        var sut = CreateSut();

        sut.UserManager.Setup(x => x.FindByEmailAsync("doctor@example.com")).ReturnsAsync((ApplicationUser?)null);
        sut.UserManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Callback<ApplicationUser, string>((user, _) => user.Id = "doctor-1")
            .ReturnsAsync(IdentityResult.Success);
        sut.UserManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Doctor"))
            .ReturnsAsync(IdentityResult.Success);

        var createdDoctor = CreateDoctorEntity("doctor-1");
        sut.DoctorRepository.Setup(x => x.GetByIdAsync("doctor-1")).ReturnsAsync(createdDoctor);
        sut.DoctorRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await sut.Service.CreateDoctorAsync(new DoctorCreateDTO
        {
            FirstName = "Doc",
            LastName = "Tor",
            Email = "doctor@example.com",
            Specialization = "Oncology"
        });

        result.Should().NotBeNull();
        result.Id.Should().Be("doctor-1");
        result.Email.Should().Be("doctor@example.com");

        sut.DoctorRepository.Verify(x => x.Add(It.Is<Doctor>(d => d.UserId == "doctor-1")), Times.Once);
        sut.DoctorRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        sut.UserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Once);
        sut.UserManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Doctor"), Times.Once);
    }

    [Fact]
    public async Task UpdateDoctorAsync_WhenDoctorMissing_ReturnsNull()
    {
        var sut = CreateSut();
        sut.DoctorRepository.Setup(x => x.GetByIdAsync("missing")).ReturnsAsync((Doctor?)null);

        var result = await sut.Service.UpdateDoctorAsync("missing", new DoctorUpdateDTO());

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateDoctorAsync_WhenEmailAlreadyTaken_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        var doctor = CreateDoctorEntity("doctor-1", "doctor@example.com");

        sut.DoctorRepository.Setup(x => x.GetByIdAsync("doctor-1")).ReturnsAsync(doctor);
        sut.UserManager.Setup(x => x.FindByEmailAsync("taken@example.com"))
            .ReturnsAsync(new ApplicationUser { Id = "other-id", Email = "taken@example.com" });

        var act = async () => await sut.Service.UpdateDoctorAsync("doctor-1", new DoctorUpdateDTO
        {
            Email = "taken@example.com"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email 'taken@example.com' is already taken.");
    }

    [Fact]
    public async Task UpdateDoctorAsync_WhenValid_UpdatesAndReturnsMappedDoctor()
    {
        var sut = CreateSut();
        var doctor = CreateDoctorEntity("doctor-1", "doctor@example.com");
        var updatedDoctor = CreateDoctorEntity("doctor-1", "updated@example.com", "Updated", "Doctor", "Radiology");

        sut.DoctorRepository.SetupSequence(x => x.GetByIdAsync("doctor-1"))
            .ReturnsAsync(doctor)
            .ReturnsAsync(updatedDoctor);

        sut.UserManager.Setup(x => x.FindByEmailAsync("updated@example.com")).ReturnsAsync((ApplicationUser?)null);
        sut.UserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        sut.DoctorRepository.Setup(x => x.Update(It.IsAny<Doctor>()));
        sut.DoctorRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await sut.Service.UpdateDoctorAsync("doctor-1", new DoctorUpdateDTO
        {
            FirstName = "Updated",
            Email = "updated@example.com",
            Specialization = "Radiology"
        });

        result.Should().NotBeNull();
        result!.Id.Should().Be("doctor-1");
        result.Email.Should().Be("updated@example.com");
        result.Specialization.Should().Be("Radiology");

        sut.UserManager.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Once);
        sut.DoctorRepository.Verify(x => x.Update(It.IsAny<Doctor>()), Times.Once);
        sut.DoctorRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteDoctorAsync_WhenValid_DeactivatesUser()
    {
        var sut = CreateSut();
        var doctor = CreateDoctorEntity("doctor-1");

        sut.DoctorRepository.Setup(x => x.GetByIdAsync("doctor-1")).ReturnsAsync(doctor);
        sut.UserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        sut.DoctorRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        await sut.Service.DeleteDoctorAsync("doctor-1");

        sut.UserManager.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.IsActive == false)), Times.Once);
        sut.DoctorRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task HardDeleteDoctorAsync_WhenValid_DeletesUser()
    {
        var sut = CreateSut();
        var doctor = CreateDoctorEntity("doctor-1");

        sut.DoctorRepository.Setup(x => x.GetByIdAsync("doctor-1")).ReturnsAsync(doctor);
        sut.UserManager.Setup(x => x.DeleteAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        await sut.Service.HardDeleteDoctorAsync("doctor-1");

        sut.UserManager.Verify(x => x.DeleteAsync(It.Is<ApplicationUser>(u => u.Id == "doctor-1")), Times.Once);
    }

    private static Doctor CreateDoctorEntity(
        string id,
        string email = "doctor@example.com",
        string firstName = "Doc",
        string lastName = "Tor",
        string? specialization = "Oncology")
    {
        var user = new ApplicationUser
        {
            Id = id,
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = "1234567890",
            Address = "Clinic street",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        return new Doctor
        {
            UserId = id,
            User = user,
            Specialization = specialization,
            LicenseNumber = "LIC-123",
            YearsOfExperience = 8,
            IsVerified = true,
            NationalIdImage = "national-id.png"
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
        var doctorRepository = new Mock<IDoctorRepository>();
        var logger = new Mock<ILogger<DoctorService>>();
        var mapper = new Mock<IMapper>();

        mapper.Setup(x => x.Map<DoctorResponseDTO>(It.IsAny<Doctor>()))
            .Returns((Doctor source) => MapDoctor(source));
        mapper.Setup(x => x.Map<IEnumerable<DoctorResponseDTO>>(It.IsAny<IEnumerable<Doctor>>()))
            .Returns((IEnumerable<Doctor> sources) => sources.Select(MapDoctor).ToArray());
        mapper.Setup(x => x.Map<ApplicationUser>(It.IsAny<DoctorCreateDTO>()))
            .Returns((DoctorCreateDTO source) => new ApplicationUser
            {
                Email = source.Email,
                UserName = source.Email,
                FirstName = source.FirstName,
                LastName = source.LastName,
                PhoneNumber = source.PhoneNumber,
                Address = source.Address,
                ImageUrl = source.ImageUrl,
                DateOfBirth = source.DateOfBirth,
                Gender = source.Gender,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        mapper.Setup(x => x.Map<Doctor>(It.IsAny<DoctorCreateDTO>()))
            .Returns((DoctorCreateDTO source) => new Doctor
            {
                Specialization = source.Specialization,
                LicenseNumber = source.LicenseNumber,
                YearsOfExperience = source.YearsOfExperience,
                IsVerified = false,
                NationalIdImage = "national-id.png"
            });
        mapper.Setup(x => x.Map(It.IsAny<DoctorUpdateDTO>(), It.IsAny<ApplicationUser>()))
            .Callback<DoctorUpdateDTO, ApplicationUser>((source, destination) =>
            {
                if (!string.IsNullOrEmpty(source.FirstName)) destination.FirstName = source.FirstName;
                if (!string.IsNullOrEmpty(source.LastName)) destination.LastName = source.LastName;
                if (!string.IsNullOrEmpty(source.Email))
                {
                    destination.Email = source.Email;
                    destination.UserName = source.Email;
                }
                if (!string.IsNullOrEmpty(source.PhoneNumber)) destination.PhoneNumber = source.PhoneNumber;
                if (!string.IsNullOrEmpty(source.Address)) destination.Address = source.Address;
                if (!string.IsNullOrEmpty(source.ImageUrl)) destination.ImageUrl = source.ImageUrl;
                if (source.DateOfBirth.HasValue) destination.DateOfBirth = source.DateOfBirth;
                if (source.Gender.HasValue) destination.Gender = source.Gender;
                if (source.IsActive.HasValue) destination.IsActive = source.IsActive.Value;
                destination.UpdatedAt = DateTime.UtcNow;
            });
        mapper.Setup(x => x.Map(It.IsAny<DoctorUpdateDTO>(), It.IsAny<Doctor>()))
            .Callback<DoctorUpdateDTO, Doctor>((source, destination) =>
            {
                if (!string.IsNullOrEmpty(source.Specialization)) destination.Specialization = source.Specialization;
                if (!string.IsNullOrEmpty(source.LicenseNumber)) destination.LicenseNumber = source.LicenseNumber;
                if (source.YearsOfExperience.HasValue) destination.YearsOfExperience = source.YearsOfExperience;
                if (source.IsVerified.HasValue) destination.IsVerified = source.IsVerified.Value;
            });

        unitOfWork.SetupGet(x => x.DoctorsRepository).Returns(doctorRepository.Object);

        var service = new DoctorService(unitOfWork.Object, userManager.Object, logger.Object, mapper.Object);

        return new SutContext(service, userManager, unitOfWork, doctorRepository);
    }

    private sealed record SutContext(
        DoctorService Service,
        Mock<UserManager<ApplicationUser>> UserManager,
        Mock<IUnitOfWork> UnitOfWork,
        Mock<IDoctorRepository> DoctorRepository);

    private static DoctorResponseDTO MapDoctor(Doctor source) => new()
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
        Specialization = source.Specialization,
        LicenseNumber = source.LicenseNumber,
        YearsOfExperience = source.YearsOfExperience,
        IsVerified = source.IsVerified,
        CreatedAt = source.User?.CreatedAt ?? default,
        UpdatedAt = source.User?.UpdatedAt ?? default
    };
}
