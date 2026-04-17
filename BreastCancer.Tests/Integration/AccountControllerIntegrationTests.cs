using System.Net;
using System.Net.Http.Json;
using BreastCancer.Controllers;
using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Service.Interface;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BreastCancer.Tests.Integration;

public class AccountControllerIntegrationTests
{
    [Fact]
    public async Task Login_ReturnsOk_WhenCredentialsAreValid()
    {
        var fake = new FakeAccountService
        {
            LoginResponse = new TokenResponseDTO
            {
                IsSuccess = true,
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresTime = DateTime.UtcNow.AddMinutes(30)
            }
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/api/Account/Login", new LoginDTO
        {
            Email = "test@example.com",
            Password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<TokenResponseDTO>();
        payload.Should().NotBeNull();
        payload!.AccessToken.Should().Be("access-token");
        payload.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenServiceRejectsCredentials()
    {
        var fake = new FakeAccountService
        {
            LoginResponse = new TokenResponseDTO
            {
                IsSuccess = false,
                Errors = new[] { "Invalid email or password." }
            }
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/api/Account/Login", new LoginDTO
        {
            Email = "test@example.com",
            Password = "wrong"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Invalid email or password.");
    }

    [Fact]
    public async Task RefreshToken_ReturnsBadRequest_WhenRequiredFieldMissing()
    {
        var fake = new FakeAccountService
        {
            RefreshTokenResponse = new TokenResponseDTO
            {
                IsSuccess = true,
                AccessToken = "unused",
                RefreshToken = "unused",
                ExpiresTime = DateTime.UtcNow.AddMinutes(10)
            }
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/api/Account/RefreshToken", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConfirmEmail_ReturnsOk_WhenServiceSucceeds()
    {
        var fake = new FakeAccountService
        {
            ConfirmEmailResponse = (true, Array.Empty<string>())
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/api/Account/ConfirmEmail", new ConfirmEmailDTO
        {
            Email = "test@example.com",
            Code = "123456"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Email confirmed successfully");
    }

    private static async Task<WebApplication> BuildAppAsync(FakeAccountService fakeAccountService)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(AccountController).Assembly);

        builder.Services.AddSingleton<IAccountService>(fakeAccountService);

        var app = builder.Build();
        app.MapControllers();

        await app.StartAsync();
        return app;
    }

    private sealed class FakeAccountService : IAccountService
    {
        public TokenResponseDTO LoginResponse { get; set; } = new() { IsSuccess = true };
        public TokenResponseDTO RefreshTokenResponse { get; set; } = new() { IsSuccess = true };
        public (bool IsSuccess, IEnumerable<string> Errors) ConfirmEmailResponse { get; set; } = (true, Array.Empty<string>());
        public (bool IsSuccess, IEnumerable<string> Errors) GenericSuccessTuple { get; set; } = (true, Array.Empty<string>());
        public bool LogoutResult { get; set; } = true;

        public Task<TokenResponseDTO> LoginAsync(LoginDTO loginDTO) => Task.FromResult(LoginResponse);

        public Task<(bool IsSuccess, IEnumerable<string> Errors)> DoctorRegister(DoctorRegisterDTO DoctorFromRequest)
            => Task.FromResult(GenericSuccessTuple);

        public Task<(bool IsSuccess, IEnumerable<string> Errors)> CaregiverRegister(CaregiverRegisterDTO CaregiverFromRequest)
            => Task.FromResult(GenericSuccessTuple);

        public Task<(bool IsSuccess, IEnumerable<string> Errors)> PatientRegister(PatientRegisterDTO PatientFromRequest)
            => Task.FromResult(GenericSuccessTuple);

        public Task<bool> LogoutAsync(LogoutDTO dto) => Task.FromResult(LogoutResult);

        public Task<TokenResponseDTO> RefreshTokenAsync(RefreshTokenDTO refreshToken)
            => Task.FromResult(RefreshTokenResponse);

        public Task<(bool IsSuccess, IEnumerable<string> Errors)> ConfirmEmailAsync(ConfirmEmailDTO Confirm)
            => Task.FromResult(ConfirmEmailResponse);

        public Task<(bool IsSuccess, IEnumerable<string> Errors)> ResendConfirmationCodeAsync(string Email)
            => Task.FromResult(GenericSuccessTuple);

        public Task<(bool IsSuccess, IEnumerable<string> Errors)> ResetPasswordAsync(ResetPasswordDTO resetPassword)
            => Task.FromResult(GenericSuccessTuple);

        public Task<(bool IsSuccess, IEnumerable<string> Errors)> SendForgetPasswordCodeAsync(string Email)
            => Task.FromResult(GenericSuccessTuple);

        public Task<(bool IsSuccess, IEnumerable<string> Errors)> ForgetPasswordAsync(ForgetPasswordDTO forgetPassword)
            => Task.FromResult(GenericSuccessTuple);
    }
}
