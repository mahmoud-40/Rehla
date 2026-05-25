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
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BreastCancer.Tests.Integration;

public class NotificationControllerIntegrationTests
{
    [Fact]
    public async Task GetNotifications_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        await using var app = await BuildAppAsync(new FakeNotificationService());
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, " ");

        var response = await client.GetAsync("/api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNotifications_ReturnsOk_WithPagination()
    {
        var fake = new FakeNotificationService();
        fake.Seed("user-1", Enumerable.Range(1, 25).Select(i => CreateDto("user-1", i, false)).ToList());

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "user-1");

        var response = await client.GetAsync("/api/notifications?page=2&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PaginatedNotificationsResponse>();
        payload.Should().NotBeNull();
        payload!.Page.Should().Be(2);
        payload.PageSize.Should().Be(10);
        payload.TotalCount.Should().Be(25);
        payload.UnreadCount.Should().Be(25);
        payload.Items.Should().HaveCount(10);
    }

    [Fact]
    public async Task MarkAsRead_ReturnsNoContent_WhenNotificationExists()
    {
        var fake = new FakeNotificationService();
        fake.Seed("user-1", new[] { CreateDto("user-1", 1, false) });

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "user-1");

        var response = await client.PutAsync("/api/notifications/1/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        fake.GetById("user-1", 1)!.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsRead_ReturnsNotFound_WhenNotificationMissing()
    {
        var fake = new FakeNotificationService();

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "user-1");

        var response = await client.PutAsync("/api/notifications/99/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkAsRead_ReturnsNotFound_WhenNotificationBelongsToAnotherUser()
    {
        var fake = new FakeNotificationService();
        fake.Seed("user-1", new[] { CreateDto("user-1", 1, false) });

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "user-2");

        var response = await client.PutAsync("/api/notifications/1/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        fake.GetById("user-1", 1)!.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAllAsRead_ReturnsMarkedCount()
    {
        var fake = new FakeNotificationService();
        fake.Seed("user-1", new[]
        {
            CreateDto("user-1", 1, false),
            CreateDto("user-1", 2, false),
            CreateDto("user-1", 3, true)
        });

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "user-1");

        var response = await client.PutAsync("/api/notifications/read-all", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<MarkAllResponse>();
        payload.Should().NotBeNull();
        payload!.MarkedCount.Should().Be(2);
        fake.GetAll("user-1").Should().OnlyContain(n => n.IsRead);
    }

    [Fact]
    public async Task GetNotifications_ReturnsOnlyCurrentUserNotifications()
    {
        var fake = new FakeNotificationService();
        fake.Seed("user-1", new[] { CreateDto("user-1", 1, false) });
        fake.Seed("user-2", new[] { CreateDto("user-2", 2, false) });

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "user-1");

        var response = await client.GetAsync("/api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PaginatedNotificationsResponse>();
        payload!.Items.Should().ContainSingle();
        payload.Items[0].UserId.Should().Be("user-1");
        payload.TotalCount.Should().Be(1);
    }

    private static NotificationDto CreateDto(string userId, int id, bool isRead) => new()
    {
        Id = id,
        UserId = userId,
        Title = $"Title {id}",
        Message = $"Message {id}",
        Type = NotificationType.General,
        IsRead = isRead,
        CreatedAt = DateTime.UtcNow.AddMinutes(-id)
    };

    private static HttpClient CreateAuthenticatedClient(WebApplication app, string userId)
    {
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        return client;
    }

    private static async Task<WebApplication> BuildAppAsync(FakeNotificationService fakeNotificationService)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(NotificationController).Assembly);

        builder.Services.AddSingleton<INotificationService>(fakeNotificationService);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        await app.StartAsync();
        return app;
    }

    private sealed class MarkAllResponse
    {
        public int MarkedCount { get; set; }
    }

    private sealed class FakeNotificationService : INotificationService
    {
        private readonly Dictionary<string, List<NotificationDto>> _byUser = new();

        public void Seed(string userId, IEnumerable<NotificationDto> notifications)
        {
            _byUser[userId] = notifications.Select(Clone).ToList();
        }

        public NotificationDto? GetById(string userId, int id)
            => _byUser.GetValueOrDefault(userId)?.FirstOrDefault(n => n.Id == id);

        public IReadOnlyList<NotificationDto> GetAll(string userId)
            => _byUser.TryGetValue(userId, out var list) ? list : Array.Empty<NotificationDto>();

        public Task<NotificationDto> SendNotificationAsync(string userId, CreateNotificationDto payload)
        {
            var list = _byUser.GetValueOrDefault(userId) ?? new List<NotificationDto>();
            var nextId = list.Count == 0 ? 1 : list.Max(n => n.Id) + 1;
            var created = new NotificationDto
            {
                Id = nextId,
                UserId = userId,
                Title = payload.Title,
                Message = payload.Message,
                Type = payload.Type,
                TargetId = payload.TargetId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            list.Add(created);
            _byUser[userId] = list;
            return Task.FromResult(created);
        }

        public Task<PaginatedNotificationsResponse> GetUserNotificationsAsync(string userId, int page, int pageSize)
        {
            var all = GetAll(userId).OrderByDescending(n => n.CreatedAt).ToList();
            var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return Task.FromResult(new PaginatedNotificationsResponse
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = all.Count,
                UnreadCount = all.Count(n => !n.IsRead)
            });
        }

        public Task<bool> MarkAsReadAsync(int id, string userId)
        {
            var notification = GetById(userId, id);
            if (notification is null)
            {
                return Task.FromResult(false);
            }

            notification.IsRead = true;
            return Task.FromResult(true);
        }

        public Task<int> MarkAllAsReadAsync(string userId)
        {
            if (!_byUser.TryGetValue(userId, out var list))
            {
                return Task.FromResult(0);
            }

            var count = 0;
            foreach (var notification in list.Where(n => !n.IsRead))
            {
                notification.IsRead = true;
                count++;
            }

            return Task.FromResult(count);
        }

        private static NotificationDto Clone(NotificationDto source) => new()
        {
            Id = source.Id,
            UserId = source.UserId,
            Title = source.Title,
            Message = source.Message,
            Type = source.Type,
            TargetId = source.TargetId,
            IsRead = source.IsRead,
            CreatedAt = source.CreatedAt
        };
    }
}
