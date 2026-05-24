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

public class DoctorControllerIntegrationTests
{
    [Fact]
    public async Task GetAllDoctors_ReturnsOk_WhenServiceReturnsDoctors()
    {
        var fake = new FakeDoctorService
        {
            Doctors = new[]
            {
                new DoctorResponseDTO { Id = "doctor-1", Email = "doctor1@example.com", FirstName = "Doc", LastName = "One" },
                new DoctorResponseDTO { Id = "doctor-2", Email = "doctor2@example.com", FirstName = "Doc", LastName = "Two" }
            }
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/Doctor?pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<DoctorResponseDTO[]>();
        payload.Should().NotBeNull();
        payload!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDoctorById_ReturnsNotFound_WhenMissing()
    {
        var fake = new FakeDoctorService();

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/Doctor/missing");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateDoctor_ReturnsCreated_WhenValid()
    {
        var fake = new FakeDoctorService
        {
            CreatedDoctor = new DoctorResponseDTO
            {
                Id = "doctor-1",
                Email = "doctor@example.com",
                FirstName = "Doc",
                LastName = "Tor"
            }
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/api/Doctor", new DoctorCreateDTO
        {
            FirstName = "Doc",
            LastName = "Tor",
            Email = "doctor@example.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<DoctorResponseDTO>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be("doctor-1");
    }

    [Fact]
    public async Task UpdateDoctor_ReturnsOk_WhenServiceUpdates()
    {
        var fake = new FakeDoctorService
        {
            UpdatedDoctor = new DoctorResponseDTO
            {
                Id = "doctor-1",
                Email = "updated@example.com",
                FirstName = "Updated",
                LastName = "Doctor"
            }
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.PutAsJsonAsync("/api/Doctor/doctor-1", new DoctorUpdateDTO
        {
            FirstName = "Updated",
            Email = "updated@example.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<DoctorResponseDTO>();
        payload.Should().NotBeNull();
        payload!.Email.Should().Be("updated@example.com");
    }

    [Fact]
    public async Task DeleteDoctor_ReturnsNoContent_WhenSuccessful()
    {
        var fake = new FakeDoctorService();

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.DeleteAsync("/api/Doctor/doctor-1");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task HardDeleteDoctor_ReturnsNoContent_WhenSuccessful()
    {
        var fake = new FakeDoctorService();

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.DeleteAsync("/api/Doctor/doctor-1/HardDelete");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static async Task<WebApplication> BuildAppAsync(FakeDoctorService fakeDoctorService)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(DoctorController).Assembly);

        builder.Services.AddSingleton<IDoctorService>(fakeDoctorService);

        var app = builder.Build();
        app.MapControllers();

        await app.StartAsync();
        return app;
    }

    private sealed class FakeDoctorService : IDoctorService
    {
        public IEnumerable<DoctorResponseDTO> Doctors { get; set; } = Array.Empty<DoctorResponseDTO>();
        public DoctorResponseDTO? CreatedDoctor { get; set; }
        public DoctorResponseDTO? UpdatedDoctor { get; set; }

        public Task<DoctorResponseDTO?> GetDoctorByIdAsync(string id)
            => Task.FromResult(id == "missing" ? null : new DoctorResponseDTO { Id = id, Email = "doctor@example.com", FirstName = "Doc", LastName = "Tor" });

        public Task<IEnumerable<DoctorResponseDTO>> GetAllDoctorsAsync(int pageNumber = 1, int pageSize = 10)
            => Task.FromResult(Doctors);

        public Task<DoctorResponseDTO> CreateDoctorAsync(DoctorCreateDTO doctorDto)
            => Task.FromResult(CreatedDoctor ?? new DoctorResponseDTO { Id = "doctor-1", Email = doctorDto.Email, FirstName = doctorDto.FirstName, LastName = doctorDto.LastName });

        public Task<DoctorResponseDTO?> UpdateDoctorAsync(string id, DoctorUpdateDTO doctorDto)
            => Task.FromResult<DoctorResponseDTO?>(UpdatedDoctor ?? new DoctorResponseDTO { Id = id, Email = doctorDto.Email ?? "updated@example.com", FirstName = doctorDto.FirstName ?? "Updated", LastName = doctorDto.LastName ?? "Doctor" });

        public Task DeleteDoctorAsync(string id) => Task.CompletedTask;
        public Task HardDeleteDoctorAsync(string id) => Task.CompletedTask;
    }
}
