using System.Net;
using System.Net.Http.Json;
using BreastCancer.Community.Controllers;
using BreastCancer.Community.Features.Queries.GetUserPosts;
using BreastCancer.Community.DTO.response;
using BreastCancer.Enum;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BreastCancer.Tests.Integration;

public sealed class GetUserPostsIntegrationTests
{
    [Fact]
    public async Task GetUserPosts_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        var fake = new FakeGetUserPostsMediator();
        await using var app = await BuildAppAsync(fake);
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, " ");

        var response = await client.GetAsync("/api/community/user-1/posts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserPosts_ReturnsBadRequest_WhenUserIdEmpty()
    {
        var fake = new FakeGetUserPostsMediator();
        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "viewer-1");

        var response = await client.GetAsync("/api/community//posts");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUserPosts_ReturnsOk_WithUserPostsFromMediator()
    {
        var fake = new FakeGetUserPostsMediator();
        var now = DateTime.UtcNow;
        fake.Response = new UserPostsResponseDto
        {
            Posts = new List<PostDTO>
            {
                new()
                {
                    Id = 1,
                    Content = "Post 1",
                    AuthorId = "user-1",
                    AuthorName = "Test User",
                    AuthorRole = "Patient",
                    PostType = PostType.Story,
                    PostVisibility = PostVisibility.Public,
                    CreatedAt = now.AddMinutes(-20),
                    UpdatedAt = now.AddMinutes(-20),
                    IsEdited = false,
                    ReactionsCount = 5,
                    CommentsCount = 3,
                    IsLikedByCurrentUser = true
                },
                new()
                {
                    Id = 2,
                    Content = "Post 2",
                    AuthorId = "user-1",
                    AuthorName = "Test User",
                    AuthorRole = "Patient",
                    PostType = PostType.Question,
                    PostVisibility = PostVisibility.Public,
                    CreatedAt = now.AddMinutes(-10),
                    UpdatedAt = now.AddMinutes(-10),
                    IsEdited = false,
                    ReactionsCount = 2,
                    CommentsCount = 1,
                    IsLikedByCurrentUser = false
                }
            },
            NextCursor = null,
            Total = 2
        };

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "viewer-1");

        var response = await client.GetAsync("/api/community/user-1/posts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<UserPostsResponseDto>();
        payload.Should().NotBeNull();
        payload!.Posts.Should().HaveCount(2);
        payload.Posts.Select(p => p.Id).Should().ContainInOrder(1, 2);
        payload.Total.Should().Be(2);
        payload.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task GetUserPosts_PassesCurrentUserIdToQuery()
    {
        var fake = new FakeGetUserPostsMediator();
        fake.Response = new UserPostsResponseDto
        {
            Posts = new List<PostDTO>(),
            NextCursor = null,
            Total = 0
        };

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "current-viewer");

        var response = await client.GetAsync("/api/community/user-1/posts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        fake.LastQuery.Should().NotBeNull();
        fake.LastQuery!.CurrentUserId.Should().Be("current-viewer");
        fake.LastQuery.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task GetUserPosts_ReturnsPaginatedResults_WithCursor()
    {
        var fake = new FakeGetUserPostsMediator();
        var now = DateTime.UtcNow;
        var cursorDate = now.AddMinutes(-20).ToString("o");

        fake.Response = new UserPostsResponseDto
        {
            Posts = new List<PostDTO>
            {
                new() { Id = 20, Content = "Post 20", AuthorId = "user-1", AuthorName = "Test User", AuthorRole = "Patient", CreatedAt = now.AddMinutes(-20) },
                new() { Id = 19, Content = "Post 19", AuthorId = "user-1", AuthorName = "Test User", AuthorRole = "Patient", CreatedAt = now.AddMinutes(-21) }
            },
            NextCursor = now.AddMinutes(-21).ToString("o"),
            Total = 100
        };

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "viewer-1");

        var response = await client.GetAsync($"/api/community/user-1/posts?cursor={Uri.EscapeDataString(cursorDate)}&limit=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<UserPostsResponseDto>();
        payload.Should().NotBeNull();
        payload!.Posts.Should().HaveCount(2);
        payload.NextCursor.Should().NotBeNull();

        fake.LastQuery.Should().NotBeNull();
        fake.LastQuery!.Cursor.Should().Be(cursorDate);
        fake.LastQuery.Limit.Should().Be(2);
    }

    [Fact]
    public async Task GetUserPosts_UsesDefaultLimit_WhenLimitNotProvided()
    {
        var fake = new FakeGetUserPostsMediator();
        fake.Response = new UserPostsResponseDto
        {
            Posts = new List<PostDTO>(),
            NextCursor = null,
            Total = 0
        };

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "viewer-1");

        var response = await client.GetAsync("/api/community/user-1/posts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        fake.LastQuery.Should().NotBeNull();
        fake.LastQuery!.Limit.Should().Be(10); // Default from controller
    }

    [Fact]
    public async Task GetUserPosts_ReturnsNotFound_WhenUserDoesNotExist()
    {
        var exception = new NotFoundException("User with ID 'non-existent' was not found");
        var fake = new FakeGetUserPostsMediator { Exception = exception };

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "viewer-1");

        var response = await client.GetAsync("/api/community/non-existent/posts");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        payload.Should().NotBeNull();
        payload!["error"].Should().Contain("not found");
    }

    [Fact]
    public async Task GetUserPosts_ReturnsOk_WithEmptyPostsList()
    {
        var fake = new FakeGetUserPostsMediator();
        fake.Response = new UserPostsResponseDto
        {
            Posts = new List<PostDTO>(),
            NextCursor = null,
            Total = 0
        };

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "viewer-1");

        var response = await client.GetAsync("/api/community/user-1/posts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<UserPostsResponseDto>();
        payload.Should().NotBeNull();
        payload!.Posts.Should().BeEmpty();
        payload.Total.Should().Be(0);
        payload.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task GetUserPosts_IncludesPostMetadata_InResponse()
    {
        var fake = new FakeGetUserPostsMediator();
        var now = DateTime.UtcNow;

        fake.Response = new UserPostsResponseDto
        {
            Posts = new List<PostDTO>
            {
                new()
                {
                    Id = 1,
                    Content = "Test post",
                    AuthorId = "user-1",
                    AuthorName = "John Doe",
                    AuthorAvatarUrl = "https://example.com/avatar.jpg",
                    AuthorRole = "Patient",
                    PostType = PostType.Question,
                    PostVisibility = PostVisibility.Public,
                    ImageUrl = new List<string> { "https://example.com/image1.jpg" },
                    MediaUrls = new[] { "https://example.com/image1.jpg" },
                    CreatedAt = now,
                    UpdatedAt = now.AddMinutes(5),
                    IsEdited = true,
                    ReactionsCount = 10,
                    CommentsCount = 5,
                    IsLikedByCurrentUser = true
                }
            },
            NextCursor = null,
            Total = 1
        };

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "viewer-1");

        var response = await client.GetAsync("/api/community/user-1/posts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<UserPostsResponseDto>();
        payload.Should().NotBeNull();

        var post = payload!.Posts.First();
        post.Id.Should().Be(1);
        post.Content.Should().Be("Test post");
        post.AuthorId.Should().Be("user-1");
        post.AuthorName.Should().Be("John Doe");
        post.AuthorAvatarUrl.Should().Be("https://example.com/avatar.jpg");
        post.AuthorRole.Should().Be("Patient");
        post.PostType.Should().Be(PostType.Question);
        post.PostVisibility.Should().Be(PostVisibility.Public);
        post.MediaUrls.Should().NotBeNull().And.ContainSingle(m => m == "https://example.com/image1.jpg");
        post.IsEdited.Should().BeTrue();
        post.ReactionsCount.Should().Be(10);
        post.CommentsCount.Should().Be(5);
        post.IsLikedByCurrentUser.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserPosts_HandlesMultiplePagesCorrectly()
    {
        var fake = new FakeGetUserPostsMediator();
        var now = DateTime.UtcNow;

        // First call with cursor1 to get page 2
        fake.SetResponseForQuery(
            "user-1", 10, now.AddMinutes(-20).ToString("o"),
            new UserPostsResponseDto
            {
                Posts = new List<PostDTO>
                {
                    new() { Id = 11, Content = "Post 11", AuthorId = "user-1", AuthorName = "Test User", AuthorRole = "Patient" },
                    new() { Id = 12, Content = "Post 12", AuthorId = "user-1", AuthorName = "Test User", AuthorRole = "Patient" }
                },
                NextCursor = now.AddMinutes(-21).ToString("o"),
                Total = 50
            }
        );

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "viewer-1");

        var response = await client.GetAsync($"/api/community/user-1/posts?cursor={Uri.EscapeDataString(now.AddMinutes(-20).ToString("o"))}&limit=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<UserPostsResponseDto>();
        payload.Should().NotBeNull();
        payload!.Posts.Should().HaveCount(2);
        payload.Total.Should().Be(50);
        payload.NextCursor.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUserPosts_AllowsAnyAuthenticatedUser_ToViewPublicPosts()
    {
        var fake = new FakeGetUserPostsMediator();
        fake.Response = new UserPostsResponseDto
        {
            Posts = new List<PostDTO> { new() { Id = 1, Content = "Public post", AuthorId = "user-1" } },
            NextCursor = null,
            Total = 1
        };

        await using var app = await BuildAppAsync(fake);

        // Viewer 1
        using var client1 = CreateAuthenticatedClient(app, "viewer-1");
        var response1 = await client1.GetAsync("/api/community/user-1/posts");
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Viewer 2
        using var client2 = CreateAuthenticatedClient(app, "viewer-2");
        var response2 = await client2.GetAsync("/api/community/user-1/posts");
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static HttpClient CreateAuthenticatedClient(WebApplication app, string userId)
    {
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        return client;
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

    private sealed class FakeGetUserPostsMediator : IMediator
    {
        private readonly Dictionary<(string, int, string), UserPostsResponseDto> _responses = new();

        public GetUserPostsQuery? LastQuery { get; private set; }
        public Exception? Exception { get; set; }

        public UserPostsResponseDto Response { get; set; } = new UserPostsResponseDto
        {
            Posts = new List<PostDTO>(),
            NextCursor = null,
            Total = 0
        };

        public void SetResponseForQuery(string userId, int limit, string cursor, UserPostsResponseDto response)
        {
            _responses[(userId, limit, cursor)] = response;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is GetUserPostsQuery query)
            {
                LastQuery = query;
                if (Exception is not null)
                {
                    return Task.FromException<TResponse>(Exception);
                }

                var key = (query.UserId, query.Limit, query.Cursor ?? "");
                var response = _responses.TryGetValue(key, out var customResponse) ? customResponse : Response;
                return Task.FromResult((TResponse)(object)response);
            }

            return Task.FromResult(default(TResponse)!);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            if (request is GetUserPostsQuery query)
            {
                LastQuery = query;
                if (Exception is not null)
                {
                    return Task.FromException<object?>(Exception);
                }

                var key = (query.UserId, query.Limit, query.Cursor ?? "");
                var response = _responses.TryGetValue(key, out var customResponse) ? customResponse : Response;
                return Task.FromResult<object?>(response);
            }

            return Task.FromResult<object?>(null);
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;

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
