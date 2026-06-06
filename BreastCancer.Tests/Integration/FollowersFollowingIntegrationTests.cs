using System.Net;
using System.Net.Http.Json;
using BreastCancer.Community.Controllers;
using BreastCancer.Community.Features.Queries.GetFollowers;
using BreastCancer.Community.Features.Queries.GetFollowing;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Rehla.Community.DTO.response;
using Xunit;

namespace BreastCancer.Tests.Integration;

public class FollowersFollowingIntegrationTests
{
    [Fact]
    public async Task GetFollowers_ReturnsOk_WithFollowersFromMediator()
    {
        var fake = new FakeFollowerMediator();
        fake.FollowersResponse = new PaginatedFollowerDto
        {
            Followers = new List<FollowerDto>
            {
                new() { UserId = "follower-1", Name = "Follower One", Role = "Patient", AvatarUrl = "url1" },
                new() { UserId = "follower-2", Name = "Follower Two", Role = "Doctor", AvatarUrl = "url2" }
            },
            Total = 2,
            NextCursor = null
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/community/user-1/followers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PaginatedFollowerDto>();
        payload.Should().NotBeNull();
        payload!.Followers.Should().HaveCount(2);
        payload.Total.Should().Be(2);
        payload.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task GetFollowers_PassesCursorToMediator()
    {
        var fake = new FakeFollowerMediator();
        fake.FollowersResponse = new PaginatedFollowerDto
        {
            Followers = new List<FollowerDto>(),
            Total = 0,
            NextCursor = null
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();
        var cursor = "2026-06-06T12:00:00Z";

        var response = await client.GetAsync($"/api/community/user-1/followers?cursor={cursor}&limit=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        fake.LastGetFollowersQuery.Should().NotBeNull();
        fake.LastGetFollowersQuery!.Cursor.Should().Be(cursor);
        fake.LastGetFollowersQuery!.Limit.Should().Be(10);
        fake.LastGetFollowersQuery!.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task GetFollowers_DefaultsLimit_To20()
    {
        var fake = new FakeFollowerMediator();
        fake.FollowersResponse = new PaginatedFollowerDto
        {
            Followers = new List<FollowerDto>(),
            Total = 0,
            NextCursor = null
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/community/user-1/followers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        fake.LastGetFollowersQuery.Should().NotBeNull();
        fake.LastGetFollowersQuery!.Limit.Should().Be(20);
    }

    [Fact]
    public async Task GetFollowers_ReturnsEmptyFollowers_WhenNoFollowers()
    {
        var fake = new FakeFollowerMediator();
        fake.FollowersResponse = new PaginatedFollowerDto
        {
            Followers = new List<FollowerDto>(),
            Total = 0,
            NextCursor = null
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/community/user-1/followers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PaginatedFollowerDto>();
        payload.Should().NotBeNull();
        payload!.Followers.Should().BeEmpty();
        payload.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetFollowers_ReturnsNextCursor_WhenMoreDataAvailable()
    {
        var fake = new FakeFollowerMediator();
        var nextCursor = "2026-06-06T12:00:00Z";
        fake.FollowersResponse = new PaginatedFollowerDto
        {
            Followers = new List<FollowerDto>
            {
                new() { UserId = "follower-1", Name = "Follower One", Role = "Patient" }
            },
            Total = 5,
            NextCursor = nextCursor
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/community/user-1/followers?limit=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PaginatedFollowerDto>();
        payload.Should().NotBeNull();
        payload!.NextCursor.Should().Be(nextCursor);
    }

    // GetFollowing endpoint tests
    [Fact]
    public async Task GetFollowing_ReturnsOk_WithFollowingFromMediator()
    {
        var fake = new FakeFollowerMediator();
        fake.FollowingResponse = new PaginatedFollowerDto
        {
            Followers = new List<FollowerDto>
            {
                new() { UserId = "following-1", Name = "Following One", Role = "Patient", AvatarUrl = "url1" },
                new() { UserId = "following-2", Name = "Following Two", Role = "Doctor", AvatarUrl = "url2" },
                new() { UserId = "following-3", Name = "Following Three", Role = "Caregiver", AvatarUrl = "url3" }
            },
            Total = 3,
            NextCursor = null
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/community/user-1/followings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PaginatedFollowerDto>();
        payload.Should().NotBeNull();
        payload!.Followers.Should().HaveCount(3);
        payload.Total.Should().Be(3);
        payload.Followers.Select(f => f.Role).Should().Contain(new[] { "Patient", "Doctor", "Caregiver" });
    }

    [Fact]
    public async Task GetFollowing_PassesCursorAndLimitToMediator()
    {
        var fake = new FakeFollowerMediator();
        fake.FollowingResponse = new PaginatedFollowerDto
        {
            Followers = new List<FollowerDto>(),
            Total = 0,
            NextCursor = null
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();
        var cursor = "2026-06-06T10:00:00Z";

        var response = await client.GetAsync($"/api/community/user-1/followings?cursor={cursor}&limit=15");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        fake.LastGetFollowingQuery.Should().NotBeNull();
        fake.LastGetFollowingQuery!.Cursor.Should().Be(cursor);
        fake.LastGetFollowingQuery!.Limit.Should().Be(15);
        fake.LastGetFollowingQuery!.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task GetFollowing_DefaultsLimit_To20()
    {
        var fake = new FakeFollowerMediator();
        fake.FollowingResponse = new PaginatedFollowerDto
        {
            Followers = new List<FollowerDto>(),
            Total = 0,
            NextCursor = null
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/community/user-1/followings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        fake.LastGetFollowingQuery.Should().NotBeNull();
        fake.LastGetFollowingQuery!.Limit.Should().Be(20);
    }

    [Fact]
    public async Task GetFollowing_ReturnsEmptyFollowing_WhenNoFollowing()
    {
        var fake = new FakeFollowerMediator();
        fake.FollowingResponse = new PaginatedFollowerDto
        {
            Followers = new List<FollowerDto>(),
            Total = 0,
            NextCursor = null
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/community/user-1/followings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PaginatedFollowerDto>();
        payload.Should().NotBeNull();
        payload!.Followers.Should().BeEmpty();
        payload.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetFollowing_ReturnsNextCursor_WhenMoreDataAvailable()
    {
        var fake = new FakeFollowerMediator();
        var nextCursor = "2026-06-06T11:00:00Z";
        fake.FollowingResponse = new PaginatedFollowerDto
        {
            Followers = new List<FollowerDto>
            {
                new() { UserId = "following-1", Name = "Following One", Role = "Patient" },
                new() { UserId = "following-2", Name = "Following Two", Role = "Doctor" }
            },
            Total = 10,
            NextCursor = nextCursor
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/community/user-1/followings?limit=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PaginatedFollowerDto>();
        payload.Should().NotBeNull();
        payload!.NextCursor.Should().Be(nextCursor);
    }

    [Fact]
    public async Task GetFollowing_CanHandleLargeLimitValues()
    {
        var fake = new FakeFollowerMediator();
        fake.FollowingResponse = new PaginatedFollowerDto
        {
            Followers = new List<FollowerDto>(),
            Total = 0,
            NextCursor = null
        };

        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/community/user-1/followings?limit=1000");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        fake.LastGetFollowingQuery.Should().NotBeNull();
        fake.LastGetFollowingQuery!.Limit.Should().Be(1000);
    }

    private static async Task<WebApplication> BuildAppAsync(IMediator mediator)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(CommunityController).Assembly);

        builder.Services.AddSingleton(mediator);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        await app.StartAsync();
        return app;
    }

    private sealed class FakeFollowerMediator : IMediator
    {
        public GetFollowersQuery? LastGetFollowersQuery { get; private set; }
        public GetFollowingQuery? LastGetFollowingQuery { get; private set; }

        public PaginatedFollowerDto FollowersResponse { get; set; } = new PaginatedFollowerDto
        {
            Followers = new List<FollowerDto>(),
            Total = 0,
            NextCursor = null
        };

        public PaginatedFollowerDto FollowingResponse { get; set; } = new PaginatedFollowerDto
        {
            Followers = new List<FollowerDto>(),
            Total = 0,
            NextCursor = null
        };

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is GetFollowersQuery getFollowersQuery)
            {
                LastGetFollowersQuery = getFollowersQuery;
                return Task.FromResult((TResponse)(object)FollowersResponse);
            }

            if (request is GetFollowingQuery getFollowingQuery)
            {
                LastGetFollowingQuery = getFollowingQuery;
                return Task.FromResult((TResponse)(object)FollowingResponse);
            }

            return Task.FromResult(default(TResponse)!);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            if (request is GetFollowersQuery getFollowersQuery)
            {
                LastGetFollowersQuery = getFollowersQuery;
                return Task.FromResult<object?>(FollowersResponse);
            }

            if (request is GetFollowingQuery getFollowingQuery)
            {
                LastGetFollowingQuery = getFollowingQuery;
                return Task.FromResult<object?>(FollowingResponse);
            }

            return Task.FromResult<object?>(null);
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            return AsyncStreamEmpty<TResponse>();

            static async IAsyncEnumerable<TResponse> AsyncStreamEmpty<T>()
            {
                yield break;
            }
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            return AsyncStreamEmpty();

            static async IAsyncEnumerable<object?> AsyncStreamEmpty()
            {
                yield break;
            }
        }
    }
}
