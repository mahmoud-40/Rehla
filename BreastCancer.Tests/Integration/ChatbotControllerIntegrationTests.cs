using System.Net;
using System.Net.Http.Json;
using BreastCancer.Controllers;
using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Service.Interface;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BreastCancer.Tests.Integration;

public class ChatbotControllerIntegrationTests
{
    [Fact]
    public async Task AskChatbot_ReturnsOk_WhenUserMatchesPatient()
    {
        var fake = new FakeChatbotService
        {
            Response = new ChatbotResponse { Answer = "Stay hydrated." }
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "patient-1");

        var response = await client.PostAsJsonAsync("/api/Chatbot/ask", new ChatbotAskDTO
        {
            PatientId = "patient-1",
            Question = "Any advice?"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ChatbotResponse>();
        payload.Should().NotBeNull();
        payload!.Answer.Should().Be("Stay hydrated.");
        fake.LastRequest.Should().NotBeNull();
        fake.LastRequest!.PatientId.Should().Be("patient-1");
    }

    [Fact]
    public async Task AskChatbot_ReturnsForbidden_WhenUserDiffersFromPatient()
    {
        var fake = new FakeChatbotService();

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "patient-2");

        var response = await client.PostAsJsonAsync("/api/Chatbot/ask", new ChatbotAskDTO
        {
            PatientId = "patient-1",
            Question = "Any advice?"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        fake.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task AskChatbot_ReturnsForbidden_WhenUserIdMissing()
    {
        var fake = new FakeChatbotService();

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, string.Empty);

        var response = await client.PostAsJsonAsync("/api/Chatbot/ask", new ChatbotAskDTO
        {
            PatientId = "patient-1",
            Question = "Any advice?"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        fake.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task AskChatbot_ReturnsBadRequest_WhenServiceThrowsInvalidOperation()
    {
        var fake = new FakeChatbotService
        {
            ExceptionToThrow = new InvalidOperationException("Patient diagnosis not found")
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "patient-1");

        var response = await client.PostAsJsonAsync("/api/Chatbot/ask", new ChatbotAskDTO
        {
            PatientId = "patient-1",
            Question = "Any advice?"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AskChatbot_ReturnsServerError_WhenServiceThrowsUnexpected()
    {
        var fake = new FakeChatbotService
        {
            ExceptionToThrow = new Exception("Unexpected")
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "patient-1");

        var response = await client.PostAsJsonAsync("/api/Chatbot/ask", new ChatbotAskDTO
        {
            PatientId = "patient-1",
            Question = "Any advice?"
        });

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    private static async Task<WebApplication> BuildAppAsync(FakeChatbotService fakeChatbotService)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(ChatbotController).Assembly);

        builder.Services.AddSingleton<IChatbotService>(fakeChatbotService);

        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        await app.StartAsync();
        return app;
    }

    private sealed class FakeChatbotService : IChatbotService
    {
        public ChatbotAskDTO? LastRequest { get; private set; }
        public ChatbotResponse Response { get; set; } = new() { Answer = "Default" };

        public Exception? ExceptionToThrow { get; set; }

        public Task<ChatbotResponse> AskQuestion(ChatbotAskDTO askDto)
        {
            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            LastRequest = askDto;
            return Task.FromResult(Response);
        }
    }

    
}
