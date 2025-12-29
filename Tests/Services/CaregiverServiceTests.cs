using AutoMapper;
using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;
using BreastCancer.Service.Implementation;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace BreastCancer.Tests.Services
{
    public class CaregiverServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<ICaregiverRepository> _caregiverRepositoryMock;
        private readonly CaregiverService _caregiverService;

        public CaregiverServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _caregiverRepositoryMock = new Mock<ICaregiverRepository>();

            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _unitOfWorkMock.Setup(u => u.CaregiversRepository).Returns(_caregiverRepositoryMock.Object);

            _caregiverService = new CaregiverService(
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _userManagerMock.Object);
        }

        #region GetAllCaregivers Tests

        [Fact]
        public async Task GetAllCaregivers_WhenCaregiversExist_ReturnsListOfCaregivers()
        {
            var caregivers = CreateTestCaregivers();
            var caregiverResponses = new List<CaregiverResponse>
            {
                new CaregiverResponse { Id = "user1", Name = "Mahmoud Abdulmawla", Email = "Mahmoud@test.com", PatientId = "patient1" },
                new CaregiverResponse { Id = "user2", Name = "Mahmoud Abdulmawla", Email = "Mahmoud@test.com", PatientId = "patient1" }
            };

            _caregiverRepositoryMock
                .Setup(r => r.FilterAsync(It.IsAny<Expression<Func<Caregiver, bool>>>(), null, null, null))
                .ReturnsAsync(caregivers);

            _mapperMock
                .Setup(m => m.Map<IEnumerable<CaregiverResponse>>(It.IsAny<IEnumerable<Caregiver>>()))
                .Returns(caregiverResponses);

            var result = await _caregiverService.GetAllCaregivers();

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.First().Name.Should().Be("Mahmoud Abdulmawla");
        }

        [Fact]
        public async Task GetAllCaregivers_WhenNoCaregiversExist_ReturnsEmptyCollection()
        {
            _caregiverRepositoryMock
                .Setup(r => r.FilterAsync(It.IsAny<Expression<Func<Caregiver, bool>>>(), null, null, null))
                .ReturnsAsync(new List<Caregiver>());

            var result = await _caregiverService.GetAllCaregivers();

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllCaregivers_WhenRepositoryReturnsNull_ReturnsEmptyCollection()
        {
            _caregiverRepositoryMock
                .Setup(r => r.FilterAsync(It.IsAny<Expression<Func<Caregiver, bool>>>(), null, null, null))
                .ReturnsAsync((IEnumerable<Caregiver>)null!);

            var result = await _caregiverService.GetAllCaregivers();

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region GetCaregiverById Tests

        [Fact]
        public async Task GetCaregiverById_WhenCaregiverExists_ReturnsCaregiverResponse()
        {
            var caregiverId = "user1";
            var caregiver = CreateTestCaregiver(caregiverId);
            var caregiverResponse = new CaregiverResponse
            {
                Id = caregiverId,
                Name = "Mahmoud Abdulmawla",
                Email = "Mahmoud@test.com",
                PatientId = "patient1"
            };

            _caregiverRepositoryMock
                .Setup(r => r.GetByIdAsync(caregiverId))
                .ReturnsAsync(caregiver);

            _mapperMock
                .Setup(m => m.Map<CaregiverResponse>(It.IsAny<Caregiver>()))
                .Returns(caregiverResponse);

            var result = await _caregiverService.GetCaregiverById(caregiverId);

            result.Should().NotBeNull();
            result.Id.Should().Be(caregiverId);
            result.Name.Should().Be("Mahmoud Abdulmawla");
        }

        [Fact]
        public async Task GetCaregiverById_WhenCaregiverAbdulmawlasNotExist_ThrowsException()
        {
            var caregiverId = "nonexistent";

            _caregiverRepositoryMock
                .Setup(r => r.GetByIdAsync(caregiverId))
                .ReturnsAsync((Caregiver)null!);

            await Assert.ThrowsAsync<Exception>(() => _caregiverService.GetCaregiverById(caregiverId));
        }

        [Fact]
        public async Task GetCaregiverById_WhenCaregiverAbdulmawlasNotExist_ThrowsExceptionWithCorrectMessage()
        {
            var caregiverId = "nonexistent";

            _caregiverRepositoryMock
                .Setup(r => r.GetByIdAsync(caregiverId))
                .ReturnsAsync((Caregiver)null!);

            var exception = await Assert.ThrowsAsync<Exception>(() => _caregiverService.GetCaregiverById(caregiverId));

            exception.Message.Should().Be("Caregiver not found.");
        }

        #endregion

        #region GetCaregiverByPatientId Tests

        [Fact]
        public async Task GetCaregiverByPatientId_WhenCaregiversExist_ReturnsCaregiversList()
        {
            var patientId = "patient1";
            var caregivers = CreateTestCaregivers().Where(c => c.PatientId == patientId).ToList();
            var caregiverResponses = new List<CaregiverResponse>
            {
                new CaregiverResponse { Id = "user1", Name = "Mahmoud Abdulmawla", Email = "Mahmoud@test.com", PatientId = patientId }
            };

            _caregiverRepositoryMock
                .Setup(r => r.FilterAsync(It.IsAny<Expression<Func<Caregiver, bool>>>(), null, null, null))
                .ReturnsAsync(caregivers);

            _mapperMock
                .Setup(m => m.Map<IEnumerable<CaregiverResponse>>(It.IsAny<IEnumerable<Caregiver>>()))
                .Returns(caregiverResponses);

            var result = await _caregiverService.GetCaregiverByPatientId(patientId);

            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result.All(c => c.PatientId == patientId).Should().BeTrue();
        }

        [Fact]
        public async Task GetCaregiverByPatientId_WhenNoCaregiversForPatient_ReturnsEmptyCollection()
        {
            var patientId = "patient999";

            _caregiverRepositoryMock
                .Setup(r => r.FilterAsync(It.IsAny<Expression<Func<Caregiver, bool>>>(), null, null, null))
                .ReturnsAsync(new List<Caregiver>());

            var result = await _caregiverService.GetCaregiverByPatientId(patientId);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetCaregiverByPatientId_WhenRepositoryReturnsNull_ReturnsEmptyCollection()
        {
            var patientId = "patient1";

            _caregiverRepositoryMock
                .Setup(r => r.FilterAsync(It.IsAny<Expression<Func<Caregiver, bool>>>(), null, null, null))
                .ReturnsAsync((IEnumerable<Caregiver>)null!);

            var result = await _caregiverService.GetCaregiverByPatientId(patientId);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region CreateCaregiver Tests

        [Fact]
        public async Task CreateCaregiver_WithValidData_CreatesSuccessfully()
        {
            var caregiverDto = new CaregiverCreateDTO
            {
                FirstName = "Mahmoud",
                LastName = "Abdulmawla",
                Email = "Mahmoud@test.com",
                PatientId = "patient1",
                RelationshipType = "Father"
            };

            var caregiver = new Caregiver
            {
                UserId = "newUserId",
                PatientId = caregiverDto.PatientId,
                RelationshipType = caregiverDto.RelationshipType
            };

            _userManagerMock
                .Setup(u => u.FindByEmailAsync(caregiverDto.Email))
                .ReturnsAsync((ApplicationUser)null!);

            _userManagerMock
                .Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock
                .Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Caregiver"))
                .ReturnsAsync(IdentityResult.Success);

            _mapperMock
                .Setup(m => m.Map<Caregiver>(caregiverDto))
                .Returns(caregiver);

            _caregiverRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Caregiver>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(u => u.SaveAsync())
                .ReturnsAsync(1);

            await _caregiverService.CreateCaregiver(caregiverDto);

            _userManagerMock.Verify(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Once);
            _userManagerMock.Verify(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Caregiver"), Times.Once);
            _caregiverRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Caregiver>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateCaregiver_WhenEmailAlreadyExists_ThrowsException()
        {
            var caregiverDto = new CaregiverCreateDTO
            {
                FirstName = "Mahmoud",
                LastName = "Abdulmawla",
                Email = "existing@test.com",
                PatientId = "patient1"
            };

            var existingUser = new ApplicationUser { Email = caregiverDto.Email };

            _userManagerMock
                .Setup(u => u.FindByEmailAsync(caregiverDto.Email))
                .ReturnsAsync(existingUser);

            var exception = await Assert.ThrowsAsync<Exception>(() => _caregiverService.CreateCaregiver(caregiverDto));
            exception.Message.Should().Be("User with the same email already exists.");
        }

        [Fact]
        public async Task CreateCaregiver_VerifyUserPropertiesAreSetCorrectly()
        {
            var caregiverDto = new CaregiverCreateDTO
            {
                FirstName = "Mahmoud",
                LastName = "Abdulmawla",
                Email = "Mahmoud@test.com",
                PatientId = "patient1"
            };

            ApplicationUser createdUser = null!;

            _userManagerMock
                .Setup(u => u.FindByEmailAsync(caregiverDto.Email))
                .ReturnsAsync((ApplicationUser)null!);

            _userManagerMock
                .Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .Callback<ApplicationUser, string>((user, password) => createdUser = user)
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock
                .Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Caregiver"))
                .ReturnsAsync(IdentityResult.Success);

            _mapperMock
                .Setup(m => m.Map<Caregiver>(caregiverDto))
                .Returns(new Caregiver { PatientId = caregiverDto.PatientId });

            _unitOfWorkMock
                .Setup(u => u.SaveAsync())
                .ReturnsAsync(1);

            await _caregiverService.CreateCaregiver(caregiverDto);

            createdUser.Should().NotBeNull();
            createdUser.FirstName.Should().Be("Mahmoud");
            createdUser.LastName.Should().Be("Abdulmawla");
            createdUser.Email.Should().Be("Mahmoud@test.com");
            createdUser.UserName.Should().Be("Mahmoud@test.com");
        }

        [Fact]
        public async Task CreateCaregiver_GeneratesPasswordWithCorrectFormat()
        {
            var caregiverDto = new CaregiverCreateDTO
            {
                FirstName = "Mahmoud",
                LastName = "Abdulmawla",
                Email = "Mahmoud@test.com",
                PatientId = "patient1"
            };

            string capturedPassword = null!;

            _userManagerMock
                .Setup(u => u.FindByEmailAsync(caregiverDto.Email))
                .ReturnsAsync((ApplicationUser)null!);

            _userManagerMock
                .Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .Callback<ApplicationUser, string>((user, password) => capturedPassword = password)
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock
                .Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Caregiver"))
                .ReturnsAsync(IdentityResult.Success);

            _mapperMock
                .Setup(m => m.Map<Caregiver>(caregiverDto))
                .Returns(new Caregiver { PatientId = caregiverDto.PatientId });

            _unitOfWorkMock
                .Setup(u => u.SaveAsync())
                .ReturnsAsync(1);

            await _caregiverService.CreateCaregiver(caregiverDto);

            capturedPassword.Should().NotBeNullOrEmpty();
            capturedPassword.Should().EndWith("aA1!");
            capturedPassword.Length.Should().Be(12);
        }

        #endregion

        #region UpdateCaregiver Tests

        [Fact]
        public async Task UpdateCaregiver_WithValidData_UpdatesSuccessfully()
        {
            var userId = "user1";
            var updateDto = new CaregiverUpdateDTO
            {
                FirstName = "UpdatedMahmoud",
                LastName = "UpdatedAbdulmawla",
                Address = "New Address"
            };

            var existingUser = new ApplicationUser
            {
                Id = userId,
                FirstName = "Mahmoud",
                LastName = "Abdulmawla"
            };

            _userManagerMock
                .Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync(existingUser);

            _mapperMock
                .Setup(m => m.Map(updateDto, existingUser))
                .Returns(existingUser);

            _userManagerMock
                .Setup(u => u.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            await _caregiverService.UpdateCaregiver(userId, updateDto);

            _userManagerMock.Verify(u => u.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Once);
        }

        [Fact]
        public async Task UpdateCaregiver_WhenUserNotFound_ThrowsException()
        {
            var userId = "nonexistent";
            var updateDto = new CaregiverUpdateDTO { FirstName = "Updated" };

            _userManagerMock
                .Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync((ApplicationUser)null!);

            var exception = await Assert.ThrowsAsync<AggregateException>(() => _caregiverService.UpdateCaregiver(userId, updateDto));
            exception.InnerException.Should().BeOfType<Exception>();
            exception.InnerException!.Message.Should().Be("User not found.");
        }

        [Fact]
        public async Task UpdateCaregiver_WhenUpdateFails_ThrowsExceptionWithDetails()
        {
            var userId = "user1";
            var updateDto = new CaregiverUpdateDTO { FirstName = "Updated" };
            var existingUser = new ApplicationUser { Id = userId, FirstName = "Mahmoud", LastName = "Abdulmawla" };

            var identityErrors = new[] { new IdentityError { Description = "Update failed due to validation" } };

            _userManagerMock
                .Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync(existingUser);

            _mapperMock
                .Setup(m => m.Map(updateDto, existingUser))
                .Returns(existingUser);

            _userManagerMock
                .Setup(u => u.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Failed(identityErrors));

            var exception = await Assert.ThrowsAsync<Exception>(() => _caregiverService.UpdateCaregiver(userId, updateDto));
            exception.Message.Should().Contain("Update failed");
            exception.Message.Should().Contain("Update failed due to validation");
        }

        [Fact]
        public async Task UpdateCaregiver_SetsUpdatedAtTimestamp()
        {
            var userId = "user1";
            var updateDto = new CaregiverUpdateDTO { FirstName = "Updated" };
            var existingUser = new ApplicationUser
            {
                Id = userId,
                FirstName = "Mahmoud",
                LastName = "Abdulmawla",
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var beforeUpdate = DateTime.UtcNow;

            _userManagerMock
                .Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync(existingUser);

            _mapperMock
                .Setup(m => m.Map(updateDto, existingUser))
                .Returns(existingUser);

            _userManagerMock
                .Setup(u => u.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            await _caregiverService.UpdateCaregiver(userId, updateDto);

            existingUser.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
        }

        #endregion

        #region DeleteCaregiver (Soft Delete) Tests

        [Fact]
        public async Task DeleteCaregiver_WhenCaregiverExists_SoftDeletesSuccessfully()
        {
            var caregiverId = "user1";
            var caregiver = CreateTestCaregiver(caregiverId);

            _caregiverRepositoryMock
                .Setup(r => r.GetByIdAsync(caregiverId))
                .ReturnsAsync(caregiver);

            _unitOfWorkMock
                .Setup(u => u.SaveAsync())
                .ReturnsAsync(1);

            await _caregiverService.DeleteCaregiver(caregiverId);

            caregiver.User.IsActive.Should().BeFalse();
            _caregiverRepositoryMock.Verify(r => r.Update(caregiver), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteCaregiver_WhenCaregiverAbdulmawlasNotExist_ThrowsException()
        {
            var caregiverId = "nonexistent";

            _caregiverRepositoryMock
                .Setup(r => r.GetByIdAsync(caregiverId))
                .ReturnsAsync((Caregiver)null!);

            var exception = await Assert.ThrowsAsync<Exception>(() => _caregiverService.DeleteCaregiver(caregiverId));
            exception.Message.Should().Be("Caregiver not found.");
        }

        [Fact]
        public async Task DeleteCaregiver_AbdulmawlasNotRemoveFromDatabase()
        {
            var caregiverId = "user1";
            var caregiver = CreateTestCaregiver(caregiverId);

            _caregiverRepositoryMock
                .Setup(r => r.GetByIdAsync(caregiverId))
                .ReturnsAsync(caregiver);

            _unitOfWorkMock
                .Setup(u => u.SaveAsync())
                .ReturnsAsync(1);

            await _caregiverService.DeleteCaregiver(caregiverId);

            _caregiverRepositoryMock.Verify(r => r.Delete(It.IsAny<Caregiver>()), Times.Never);
        }

        #endregion

        #region HardDeleteCaregiverById Tests

        [Fact]
        public async Task HardDeleteCaregiverById_WhenCaregiverExists_DeletesPermanently()
        {
            var caregiverId = "user1";
            var caregiver = CreateTestCaregiver(caregiverId);

            _caregiverRepositoryMock
                .Setup(r => r.GetByIdAsync(caregiverId))
                .ReturnsAsync(caregiver);

            _unitOfWorkMock
                .Setup(u => u.SaveAsync())
                .ReturnsAsync(1);

            await _caregiverService.HardDeleteCaregiverById(caregiverId);

            _caregiverRepositoryMock.Verify(r => r.Delete(caregiver), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task HardDeleteCaregiverById_WhenCaregiverAbdulmawlasNotExist_ThrowsException()
        {
            var caregiverId = "nonexistent";

            _caregiverRepositoryMock
                .Setup(r => r.GetByIdAsync(caregiverId))
                .ReturnsAsync((Caregiver)null!);

            var exception = await Assert.ThrowsAsync<Exception>(() => _caregiverService.HardDeleteCaregiverById(caregiverId));
            exception.Message.Should().Be("Caregiver not found.");
        }

        [Fact]
        public async Task HardDeleteCaregiverById_CallsDeleteOnRepository()
        {
            var caregiverId = "user1";
            var caregiver = CreateTestCaregiver(caregiverId);

            _caregiverRepositoryMock
                .Setup(r => r.GetByIdAsync(caregiverId))
                .ReturnsAsync(caregiver);

            _unitOfWorkMock
                .Setup(u => u.SaveAsync())
                .ReturnsAsync(1);

            await _caregiverService.HardDeleteCaregiverById(caregiverId);

            _caregiverRepositoryMock.Verify(r => r.Update(It.IsAny<Caregiver>()), Times.Never);
            _caregiverRepositoryMock.Verify(r => r.Delete(caregiver), Times.Once);
        }

        #endregion

        #region Helper Methods

        private static Caregiver CreateTestCaregiver(string userId = "user1")
        {
            return new Caregiver
            {
                UserId = userId,
                PatientId = "patient1",
                RelationshipType = "Father",
                User = new ApplicationUser
                {
                    Id = userId,
                    FirstName = "Mahmoud",
                    LastName = "Abdulmawla",
                    Email = "Mahmoud@test.com",
                    UserName = "Mahmoud@test.com",
                    IsActive = true
                }
            };
        }

        private static List<Caregiver> CreateTestCaregivers()
        {
            return new List<Caregiver>
            {
                new Caregiver
                {
                    UserId = "user1",
                    PatientId = "patient1",
                    RelationshipType = "Father",
                    User = new ApplicationUser
                    {
                        Id = "user1",
                        FirstName = "Mahmoud",
                        LastName = "Abdulmawla",
                        Email = "Mahmoud@test.com",
                        UserName = "Mahmoud@test.com",
                        IsActive = true
                    }
                },
                new Caregiver
                {
                    UserId = "user2",
                    PatientId = "patient1",
                    RelationshipType = "Child",
                    User = new ApplicationUser
                    {
                        Id = "user2",
                        FirstName = "Mahmoud",
                        LastName = "Abdulmawla",
                        Email = "Mahmoud@test.com",
                        UserName = "Mahmoud@test.com",
                        IsActive = true
                    }
                }
            };
        }

        #endregion
    }
}
