using AutoMapper;
using BreastCancer.DTO.request;
using BreastCancer.Models;
using BreastCancer.Options;
using BreastCancer.Repository.Interface;
using BreastCancer.Service.Implementation;
using BreastCancer.Service.Interface;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BreastCancer.Tests.Unit;

public class AccountServiceTests
{
    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ReturnsFailure()
    {
        var sut = CreateSut();

        sut.UserManager
            .Setup(x => x.FindByEmailAsync("missing@example.com"))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await sut.Service.LoginAsync(new LoginDTO
        {
            Email = "missing@example.com",
            Password = "Password123!"
        });

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_WhenEmailNotConfirmed_ReturnsFailure()
    {
        var sut = CreateSut();
        var user = NewUser("user@example.com");

        sut.UserManager
            .Setup(x => x.FindByEmailAsync(user.Email!))
            .ReturnsAsync(user);
        sut.UserManager
            .Setup(x => x.IsEmailConfirmedAsync(user))
            .ReturnsAsync(false);

        var result = await sut.Service.LoginAsync(new LoginDTO
        {
            Email = user.Email!,
            Password = "Password123!"
        });

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle("Please confirm your email first");
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordInvalid_ReturnsFailure()
    {
        var sut = CreateSut();
        var user = NewUser("user@example.com");

        sut.UserManager.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        sut.UserManager.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
        sut.UserManager.Setup(x => x.CheckPasswordAsync(user, "wrong-password")).ReturnsAsync(false);

        var result = await sut.Service.LoginAsync(new LoginDTO
        {
            Email = user.Email!,
            Password = "wrong-password"
        });

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_WhenExistingRefreshTokenFound_ReusesToken()
    {
        var sut = CreateSut();
        var user = NewUser("user@example.com");
        var existingRefresh = new RefreshToken
        {
            Token = "existing-refresh-token",
            User = user,
            UserId = user.Id!,
            ExpiresAt = DateTime.UtcNow.AddDays(1).ToLocalTime(),
            IsRevoked = false
        };

        sut.UserManager.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        sut.UserManager.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
        sut.UserManager.Setup(x => x.CheckPasswordAsync(user, "Password123!")).ReturnsAsync(true);

        sut.AuthTokenService
            .Setup(x => x.GenerateToken(user))
            .ReturnsAsync(("access-token", DateTime.UtcNow.AddMinutes(30)));

        sut.RefreshTokenRepository
            .Setup(x => x.CheckForExistingValidRefreshToken(user))
            .ReturnsAsync(existingRefresh);

        var result = await sut.Service.LoginAsync(new LoginDTO
        {
            Email = user.Email!,
            Password = "Password123!"
        });

        result.IsSuccess.Should().BeTrue();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be(existingRefresh.Token);

        sut.RefreshTokenRepository.Verify(x => x.Add(It.IsAny<RefreshToken>()), Times.Never);
        sut.UnitOfWork.Verify(x => x.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_WhenRefreshTokenMissing_ReturnsFalse()
    {
        var sut = CreateSut();

        sut.RefreshTokenRepository
            .Setup(x => x.GetByTokenAsync("missing-token"))
            .ReturnsAsync((RefreshToken?)null);

        var result = await sut.Service.LogoutAsync(new LogoutDTO { RefreshToken = "missing-token" });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenExpired_ReturnsFailure()
    {
        var sut = CreateSut();
        var user = NewUser("user@example.com");

        sut.RefreshTokenRepository
            .Setup(x => x.GetByTokenAsync("expired-token"))
            .ReturnsAsync(new RefreshToken
            {
                Token = "expired-token",
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1).ToLocalTime(),
                IsRevoked = false,
                User = user,
                UserId = user.Id!
            });

        var result = await sut.Service.RefreshTokenAsync(new RefreshTokenDTO
        {
            RefreshToken = "expired-token"
        });

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle("Invalid or expired refresh token");
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenValid_ReturnsNewAccessToken()
    {
        var sut = CreateSut();
        var user = NewUser("user@example.com");

        sut.RefreshTokenRepository
            .Setup(x => x.GetByTokenAsync("valid-token"))
            .ReturnsAsync(new RefreshToken
            {
                Token = "valid-token",
                ExpiresAt = DateTime.UtcNow.AddMinutes(10).ToLocalTime(),
                IsRevoked = false,
                User = user,
                UserId = user.Id!
            });

        sut.AuthTokenService
            .Setup(x => x.GenerateToken(user))
            .ReturnsAsync(("new-access-token", DateTime.UtcNow.AddMinutes(15)));

        var result = await sut.Service.RefreshTokenAsync(new RefreshTokenDTO
        {
            RefreshToken = "valid-token"
        });

        result.IsSuccess.Should().BeTrue();
        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("valid-token");
    }

    private static ApplicationUser NewUser(string email) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Email = email,
        UserName = email,
        FirstName = "Test",
        LastName = "User"
    };

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

        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        var roleManager = new Mock<RoleManager<ApplicationRole>>(
            roleStore.Object,
            null!,
            null!,
            null!,
            null!);

        var unitOfWork = new Mock<IUnitOfWork>();
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var authTokenService = new Mock<IAuthTokenService>();
        var mapper = new Mock<IMapper>();
        var emailService = new Mock<IEmailService>();

        unitOfWork.SetupGet(x => x.RefreshTokenRepository).Returns(refreshTokenRepository.Object);

        var jwtOptions = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            SecretKey = "this-is-a-very-long-test-secret-key-123456",
            Audience = "test-audience",
            Issuer = "test-issuer",
            ExpirationTimeInMinutes = 30,
            ExpirationTimeInDays = 7
        });

        var service = new AccountService(
            userManager.Object,
            roleManager.Object,
            unitOfWork.Object,
            jwtOptions,
            authTokenService.Object,
            mapper.Object,
            emailService.Object);

        return new SutContext(
            service,
            userManager,
            roleManager,
            unitOfWork,
            refreshTokenRepository,
            authTokenService);
    }

    private sealed record SutContext(
        AccountService Service,
        Mock<UserManager<ApplicationUser>> UserManager,
        Mock<RoleManager<ApplicationRole>> RoleManager,
        Mock<IUnitOfWork> UnitOfWork,
        Mock<IRefreshTokenRepository> RefreshTokenRepository,
        Mock<IAuthTokenService> AuthTokenService);
}
