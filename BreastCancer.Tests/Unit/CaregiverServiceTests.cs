using AutoMapper;
using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Enum;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using BreastCancer.Service.Implementation;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace BreastCancer.Tests.Unit;

public class CaregiverServiceTests
{
    [Fact]
    public async Task GetAllCaregivers_WhenRepositoryReturnsActiveCaregivers_ReturnsMappedCaregivers()
    {
        var sut = CreateSut();
        var caregivers = new[]
        {
            CreateCaregiverEntity("caregiver-1"),
            CreateCaregiverEntity("caregiver-2", "second@example.com", "patient-2")
        };

        sut.CaregiverRepository
            .Setup(x => x.FilterAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Caregiver, bool>>>(), null, null, null))
            .ReturnsAsync(caregivers);

        var result = await sut.Service.GetAllCaregivers();

        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().ContainInOrder("caregiver-1", "caregiver-2");
    }

    [Fact]
    public async Task GetAllCaregivers_WhenRepositoryReturnsNone_ReturnsEmpty()
    {
        var sut = CreateSut();

        sut.CaregiverRepository
            .Setup(x => x.FilterAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Caregiver, bool>>>(), null, null, null))
            .ReturnsAsync(Array.Empty<Caregiver>());

        var result = await sut.Service.GetAllCaregivers();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCaregiverById_WhenCaregiverExists_ReturnsMappedCaregiver()
    {
        var sut = CreateSut();
        var caregiver = CreateCaregiverEntity("caregiver-1");

        sut.CaregiverRepository.Setup(x => x.GetByIdAsync("caregiver-1")).ReturnsAsync(caregiver);

        var result = await sut.Service.GetCaregiverById("caregiver-1");

        result.Should().NotBeNull();
        result.Id.Should().Be("caregiver-1");
        result.Email.Should().Be("caregiver@example.com");
        result.PatientId.Should().Be("patient-1");
    }

    [Fact]
    public async Task GetCaregiverById_WhenMissing_ThrowsException()
    {
        var sut = CreateSut();

        sut.CaregiverRepository.Setup(x => x.GetByIdAsync("missing")).ReturnsAsync((Caregiver?)null);

        var act = async () => await sut.Service.GetCaregiverById("missing");

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Caregiver not found.");
    }

    [Fact]
    public async Task GetCaregiverByPatientId_WhenMatchesFound_ReturnsMappedCaregivers()
    {
        var sut = CreateSut();
        var caregivers = new[]
        {
            CreateCaregiverEntity("caregiver-1", patientId: "patient-1"),
            CreateCaregiverEntity("caregiver-2", "second@example.com", "patient-1")
        };

        sut.CaregiverRepository
            .Setup(x => x.FilterAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Caregiver, bool>>>(), null, null, null))
            .ReturnsAsync(caregivers);

        var result = await sut.Service.GetCaregiverByPatientId("patient-1");

        result.Should().HaveCount(2);
        result.All(x => x.PatientId == "patient-1").Should().BeTrue();
    }

    [Fact]
    public async Task CreateCaregiver_WhenEmailExists_ThrowsException()
    {
        var sut = CreateSut();

        sut.UserManager.Setup(x => x.FindByEmailAsync("caregiver@example.com"))
            .ReturnsAsync(new ApplicationUser { Id = "existing", Email = "caregiver@example.com" });

        var act = async () => await sut.Service.CreateCaregiver(new CaregiverCreateDTO
        {
            FirstName = "Care",
            LastName = "Giver",
            Email = "caregiver@example.com",
            PatientId = "patient-1"
        });

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("User with the same email already exists.");
    }

    [Fact]
    public async Task CreateCaregiver_WhenValid_CreatesCaregiverAndPersists()
    {
        var sut = CreateSut();

        sut.UserManager.Setup(x => x.FindByEmailAsync("caregiver@example.com"))
            .ReturnsAsync((ApplicationUser?)null);
        sut.UserManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Callback<ApplicationUser, string>((user, _) => user.Id = "caregiver-1")
            .ReturnsAsync(IdentityResult.Success);
        sut.UserManager
            .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Caregiver"))
            .ReturnsAsync(IdentityResult.Success);
        sut.CaregiverRepository.Setup(x => x.AddAsync(It.IsAny<Caregiver>())).Returns(Task.CompletedTask);
        sut.UnitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        await sut.Service.CreateCaregiver(new CaregiverCreateDTO
        {
            FirstName = "Care",
            LastName = "Giver",
            Email = "caregiver@example.com",
            PatientId = "patient-1",
            RelationshipType = RelationshipType.FRIEND
        });

        sut.CaregiverRepository.Verify(
            x => x.AddAsync(It.Is<Caregiver>(c => c.UserId == "caregiver-1" && c.PatientId == "patient-1")),
            Times.Once);
        sut.UnitOfWork.Verify(x => x.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateCaregiver_WhenUserMissing_ThrowsException()
    {
        var sut = CreateSut();

        sut.UserManager.Setup(x => x.FindByIdAsync("missing")).ReturnsAsync((ApplicationUser?)null);

        var act = async () => await sut.Service.UpdateCaregiver("missing", new CaregiverUpdateDTO { FirstName = "Updated" });

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("User not found.");
    }

    [Fact]
    public async Task UpdateCaregiver_WhenValid_UpdatesUser()
    {
        var sut = CreateSut();
        var user = new ApplicationUser
        {
            Id = "caregiver-1",
            FirstName = "Care",
            LastName = "Giver",
            Address = "Old address"
        };

        sut.UserManager.Setup(x => x.FindByIdAsync("caregiver-1")).ReturnsAsync(user);
        sut.UserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        await sut.Service.UpdateCaregiver("caregiver-1", new CaregiverUpdateDTO
        {
            FirstName = "Updated",
            Address = "New address"
        });

        sut.UserManager.Verify(
            x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.FirstName == "Updated" && u.Address == "New address")),
            Times.Once);
    }

    [Fact]
    public async Task DeleteCaregiver_WhenValid_SetsUserInactiveAndSaves()
    {
        var sut = CreateSut();
        var caregiver = CreateCaregiverEntity("caregiver-1");

        sut.CaregiverRepository.Setup(x => x.GetByIdAsync("caregiver-1")).ReturnsAsync(caregiver);
        sut.UnitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        await sut.Service.DeleteCaregiver("caregiver-1");

        sut.CaregiverRepository.Verify(x => x.Update(It.Is<Caregiver>(c => c.User.IsActive == false)), Times.Once);
        sut.UnitOfWork.Verify(x => x.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task HardDeleteCaregiverById_WhenValid_DeletesAndSaves()
    {
        var sut = CreateSut();
        var caregiver = CreateCaregiverEntity("caregiver-1");

        sut.CaregiverRepository.Setup(x => x.GetByIdAsync("caregiver-1")).ReturnsAsync(caregiver);
        sut.UnitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);

        await sut.Service.HardDeleteCaregiverById("caregiver-1");

        sut.CaregiverRepository.Verify(x => x.Delete(It.Is<Caregiver>(c => c.UserId == "caregiver-1")), Times.Once);
        sut.UnitOfWork.Verify(x => x.SaveAsync(), Times.Once);
    }

    private static Caregiver CreateCaregiverEntity(
        string id,
        string email = "caregiver@example.com",
        string patientId = "patient-1")
    {
        var user = new ApplicationUser
        {
            Id = id,
            Email = email,
            UserName = email,
            FirstName = "Care",
            LastName = "Giver",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        return new Caregiver
        {
            UserId = id,
            User = user,
            PatientId = patientId,
            RelationshipType = RelationshipType.FRIEND
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
        var caregiverRepository = new Mock<ICaregiverRepository>();
        var mapper = new Mock<IMapper>();

        mapper.Setup(x => x.Map<CaregiverResponse>(It.IsAny<Caregiver>()))
            .Returns((Caregiver source) => MapCaregiver(source));
        mapper.Setup(x => x.Map<IEnumerable<CaregiverResponse>>(It.IsAny<IEnumerable<Caregiver>>()))
            .Returns((IEnumerable<Caregiver> sources) => sources.Select(MapCaregiver).ToArray());
        mapper.Setup(x => x.Map<Caregiver>(It.IsAny<CaregiverCreateDTO>()))
            .Returns((CaregiverCreateDTO source) => new Caregiver
            {
                PatientId = source.PatientId,
                RelationshipType = source.RelationshipType,
                User = new ApplicationUser()
            });
        mapper.Setup(x => x.Map(It.IsAny<CaregiverUpdateDTO>(), It.IsAny<ApplicationUser>()))
            .Callback<CaregiverUpdateDTO, ApplicationUser>((source, destination) =>
            {
                if (!string.IsNullOrEmpty(source.FirstName)) destination.FirstName = source.FirstName;
                if (!string.IsNullOrEmpty(source.LastName)) destination.LastName = source.LastName;
                if (!string.IsNullOrEmpty(source.Address)) destination.Address = source.Address;
                if (!string.IsNullOrEmpty(source.ImageUrl)) destination.ImageUrl = source.ImageUrl;
            });

        unitOfWork.SetupGet(x => x.CaregiversRepository).Returns(caregiverRepository.Object);

        var service = new CaregiverService(unitOfWork.Object, mapper.Object, userManager.Object);

        return new SutContext(service, unitOfWork, caregiverRepository, userManager);
    }

    private sealed record SutContext(
        CaregiverService Service,
        Mock<IUnitOfWork> UnitOfWork,
        Mock<ICaregiverRepository> CaregiverRepository,
        Mock<UserManager<ApplicationUser>> UserManager);

    private static CaregiverResponse MapCaregiver(Caregiver source) => new()
    {
        Id = source.UserId,
        Name = source.User?.FullName ?? string.Empty,
        Email = source.User?.Email ?? string.Empty,
        PatientId = source.PatientId,
        RelationshipType = source.RelationshipType
    };
}
