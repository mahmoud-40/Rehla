using System.Net;
using System.Net.Http.Json;
using BreastCancer.Controllers;
using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Enum;
using BreastCancer.Service.Interface;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BreastCancer.Tests.Integration;

public class CaregiverControllerIntegrationTests
{
    [Fact]
    public async Task GetAllCaregivers_ReturnsOk_WhenServiceReturnsCaregivers()
    {
        var fake = new FakeCaregiverService
        {
            Caregivers = new[]
            {
                new CaregiverResponse { Id = "caregiver-1", Name = "Care Giver", Email = "caregiver1@example.com", PatientId = "patient-1" },
                new CaregiverResponse { Id = "caregiver-2", Name = "Care Giver 2", Email = "caregiver2@example.com", PatientId = "patient-2" }
            }
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/Caregiver");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<CaregiverResponse[]>();
        payload.Should().NotBeNull();
        payload!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllCaregivers_ReturnsNotFound_WhenServiceReturnsEmpty()
    {
        var fake = new FakeCaregiverService { Caregivers = Array.Empty<CaregiverResponse>() };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/Caregiver");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCaregiverById_ReturnsNotFound_WhenMissing()
    {
        var fake = new FakeCaregiverService();

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/Caregiver/missing");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCaregiverByPatientId_ReturnsOk_WhenFound()
    {
        var fake = new FakeCaregiverService
        {
            ByPatient = new[]
            {
                new CaregiverResponse { Id = "caregiver-1", Name = "Care Giver", Email = "caregiver@example.com", PatientId = "patient-1" }
            }
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/Caregiver/patient/patient-1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<CaregiverResponse[]>();
        payload.Should().NotBeNull();
        payload!.Should().ContainSingle();
    }

    [Fact]
    public async Task GetCaregiverByPatientId_ReturnsNotFound_WhenNone()
    {
        var fake = new FakeCaregiverService { ByPatient = Array.Empty<CaregiverResponse>() };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/Caregiver/patient/patient-1");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateCaregiver_ReturnsCreated_WhenValid()
    {
        var fake = new FakeCaregiverService();

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/api/Caregiver", new CaregiverCreateDTO
        {
            FirstName = "Care",
            LastName = "Giver",
            Email = "caregiver@example.com",
            PatientId = "patient-1",
            RelationshipType = RelationshipType.FRIEND
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UpdateCaregiver_ReturnsNoContent_WhenValid()
    {
        var fake = new FakeCaregiverService();

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.PutAsJsonAsync("/api/Caregiver/caregiver-1", new CaregiverUpdateDTO
        {
            FirstName = "Updated",
            Address = "New Address"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteCaregiver_ReturnsNoContent_WhenValid()
    {
        var fake = new FakeCaregiverService();

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.DeleteAsync("/api/Caregiver/caregiver-1");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task HardDeleteCaregiver_ReturnsNoContent_WhenValid()
    {
        var fake = new FakeCaregiverService();

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.DeleteAsync("/api/Caregiver/hard/caregiver-1");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static async Task<WebApplication> BuildAppAsync(FakeCaregiverService fakeCaregiverService)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(CaregiverController).Assembly);

        builder.Services.AddSingleton<ICaregiverService>(fakeCaregiverService);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        await app.StartAsync();
        return app;
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "Test";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user"),
                new Claim(ClaimTypes.Name, "integration-test-user"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "Doctor")
            };

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class FakeCaregiverService : ICaregiverService
    {
        public IEnumerable<CaregiverResponse> Caregivers { get; set; } = Array.Empty<CaregiverResponse>();
        public IEnumerable<CaregiverResponse> ByPatient { get; set; } = Array.Empty<CaregiverResponse>();

        public Task<IEnumerable<CaregiverResponse>> GetAllCaregivers() => Task.FromResult(Caregivers);

        public Task<CaregiverResponse> GetCaregiverById(string id)
        {
            if (id == "missing")
            {
                throw new Exception("Caregiver not found.");
            }

            return Task.FromResult(new CaregiverResponse
            {
                Id = id,
                Name = "Care Giver",
                Email = "caregiver@example.com",
                PatientId = "patient-1"
            });
        }

        public Task<IEnumerable<CaregiverResponse>> GetCaregiverByPatientId(string patientId)
            => Task.FromResult(ByPatient);

        public Task CreateCaregiver(CaregiverCreateDTO caregiverDto) => Task.CompletedTask;

        public Task UpdateCaregiver(string id, CaregiverUpdateDTO updateDto) => Task.CompletedTask;

        public Task DeleteCaregiver(string id) => Task.CompletedTask;

        public Task HardDeleteCaregiverById(string id) => Task.CompletedTask;
    }
}
