using System.Net;
using System.Net.Http.Json;
using BreastCancer.Controllers;
using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Service.Exceptions;
using BreastCancer.Service.Interface;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BreastCancer.Tests.Integration;

public class PatientControllerIntegrationTests
{
    [Fact]
    public async Task GetAllPatients_ReturnsOk_WhenServiceReturnsPatients()
    {
        var fake = new FakePatientService
        {
            Patients = new[]
            {
                new PatientResponseDTO { Id = "patient-1", Email = "patient1@example.com", FirstName = "Pat", LastName = "One" },
                new PatientResponseDTO { Id = "patient-2", Email = "patient2@example.com", FirstName = "Pat", LastName = "Two" }
            }
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/Patient?pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PatientResponseDTO[]>();
        payload.Should().NotBeNull();
        payload!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPatientById_ReturnsNotFound_WhenMissing()
    {
        var fake = new FakePatientService();

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/Patient/missing");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePatient_ReturnsCreated_WhenValid()
    {
        var fake = new FakePatientService
        {
            CreatedPatient = new PatientResponseDTO
            {
                Id = "patient-1",
                Email = "patient@example.com",
                FirstName = "Pat",
                LastName = "Ient"
            }
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/api/Patient", new PatientCreateDTO
        {
            FirstName = "Pat",
            LastName = "Ient",
            Email = "patient@example.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<PatientResponseDTO>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be("patient-1");
    }

    [Fact]
    public async Task UpdatePatient_ReturnsOk_WhenServiceUpdates()
    {
        var fake = new FakePatientService
        {
            UpdatedPatient = new PatientResponseDTO
            {
                Id = "patient-1",
                Email = "updated@example.com",
                FirstName = "Updated",
                LastName = "User"
            }
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.PutAsJsonAsync("/api/Patient/patient-1", new PatientUpdateDTO
        {
            FirstName = "Updated",
            Email = "updated@example.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PatientResponseDTO>();
        payload.Should().NotBeNull();
        payload!.Email.Should().Be("updated@example.com");
    }

    [Fact]
    public async Task DeletePatient_ReturnsNoContent_WhenSuccessful()
    {
        var fake = new FakePatientService();

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.DeleteAsync("/api/Patient/patient-1");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AddPatientMedicalData_ReturnsOk_WhenRequestIsValid()
    {
        var fake = new FakePatientService();

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/api/Patient/medical-data", new AddMedicalDataRequestDTO
        {
            PatientId = Guid.NewGuid(),
            Context = new PatientContext
            {
                AgeAtDiagnosis = 42,
                CancerType = "Invasive Ductal Carcinoma",
                CancerTypeDetailed = "Stage II",
                TumorStage = "T2",
                NeoplasmHistologicGrade = "G2",
                ErStatus = "Positive",
                PrStatus = "Positive",
                Her2Status = "Negative",
                Chemotherapy = true,
                HormoneTherapy = true,
                RadioTherapy = false
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        fake.MedicalDataRequestCount.Should().Be(1);
    }

    [Fact]
    public async Task AddPatientMedicalData_ReturnsNotFound_WhenPatientMissing()
    {
        var fake = new FakePatientService
        {
            AddMedicalDataException = new PatientMedicalDataNotFoundException("Patient not found")
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/api/Patient/medical-data", new AddMedicalDataRequestDTO
        {
            PatientId = Guid.NewGuid(),
            Context = new PatientContext
            {
                AgeAtDiagnosis = 35,
                CancerType = "Invasive Ductal Carcinoma",
                CancerTypeDetailed = "Stage II",
                TumorStage = "T2",
                NeoplasmHistologicGrade = "G2",
                ErStatus = "Positive",
                PrStatus = "Positive",
                Her2Status = "Negative",
                Chemotherapy = true,
                HormoneTherapy = false,
                RadioTherapy = true
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddPatientMedicalData_ReturnsBadRequest_WhenServiceRejectsRequest()
    {
        var fake = new FakePatientService
        {
            AddMedicalDataException = new ArgumentException("Patient context is required.")
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/api/Patient/medical-data", new AddMedicalDataRequestDTO
        {
            PatientId = Guid.NewGuid(),
            Context = new PatientContext
            {
                AgeAtDiagnosis = 30,
                CancerType = "Invasive Ductal Carcinoma",
                CancerTypeDetailed = "Stage II",
                TumorStage = "T2",
                NeoplasmHistologicGrade = "G2",
                ErStatus = "Positive",
                PrStatus = "Positive",
                Her2Status = "Negative",
                Chemotherapy = true,
                HormoneTherapy = false,
                RadioTherapy = true
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<WebApplication> BuildAppAsync(FakePatientService fakePatientService)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(PatientController).Assembly);

        builder.Services.AddSingleton<IPatientService>(fakePatientService);

        var app = builder.Build();
        app.MapControllers();

        await app.StartAsync();
        return app;
    }

    private sealed class FakePatientService : IPatientService
    {
        public IEnumerable<PatientResponseDTO> Patients { get; set; } = Array.Empty<PatientResponseDTO>();
        public PatientResponseDTO? CreatedPatient { get; set; }
        public PatientResponseDTO? UpdatedPatient { get; set; }
        public Exception? AddMedicalDataException { get; set; }
        public int MedicalDataRequestCount { get; private set; }

        public Task<PatientResponseDTO?> GetPatientByIdAsync(string id)
            => Task.FromResult(id == "missing" ? null : new PatientResponseDTO { Id = id, Email = "patient@example.com", FirstName = "Pat", LastName = "Ient" });

        public Task<IEnumerable<PatientResponseDTO>> GetAllPatientsAsync(int pageNumber = 1, int pageSize = 10)
            => Task.FromResult(Patients);

        public Task<PatientResponseDTO> CreatePatientAsync(PatientCreateDTO patientDto)
            => Task.FromResult(CreatedPatient ?? new PatientResponseDTO { Id = "patient-1", Email = patientDto.Email, FirstName = patientDto.FirstName, LastName = patientDto.LastName });

        public Task<PatientResponseDTO?> UpdatePatientAsync(string id, PatientUpdateDTO patientDto)
            => Task.FromResult<PatientResponseDTO?>(UpdatedPatient ?? new PatientResponseDTO { Id = id, Email = patientDto.Email ?? "updated@example.com", FirstName = patientDto.FirstName ?? "Updated", LastName = patientDto.LastName ?? "User" });

        public Task DeletePatientAsync(string id) => Task.CompletedTask;
        public Task HardDeletePatientAsync(string id) => Task.CompletedTask;
        public Task AddPatientMedicalDataAsync(AddMedicalDataRequestDTO request)
        {
            if (AddMedicalDataException != null)
            {
                throw AddMedicalDataException;
            }

            MedicalDataRequestCount++;
            return Task.CompletedTask;
        }
    }
}
