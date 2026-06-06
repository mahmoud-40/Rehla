using System.Net;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Linq;
using BreastCancer.Community.Controllers;
using BreastCancer.Community.DTO.request;
using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Features.DeletePost;
using BreastCancer.Community.Features.Feed;
using BreastCancer.Community.Features.GetPost;
using BreastCancer.Community.Features.UpdatePost;
using BreastCancer.Community.Features.Commands.AddReaction;
using BreastCancer.Enum;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BreastCancer.Tests.Integration;

public class CommunityControllerIntegrationTests
{
    [Fact]
    public async Task GetFeed_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        await using var app = await BuildAppAsync(new FakeMediator());
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, " ");

        var response = await client.GetAsync("/api/community/feed");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetFeed_ReturnsOk_WithFeedFromMediator()
    {
        var fake = new FakeMediator();
        fake.Response = new FeedResponseDto
        {
            Posts = new List<PostDTO>
            {
                new() { Id = 10 },
                new() { Id = 20 },
                new() { Id = 30 }
            },
            NextCursor = 30
        };

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "user-1");

        var response = await client.GetAsync("/api/community/feed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<FeedResponseDto>();
        payload.Should().NotBeNull();
        payload!.Posts.Select(p => p.Id).Should().ContainInOrder(new[] { 10, 20, 30 });
        payload.NextCursor.Should().Be(30);
    }

    [Fact]
    public async Task GetFeed_ClampsLimit_ToMax50_BeforeSendingQuery()
    {
        var fake = new FakeMediator();
        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "user-1");

        var response = await client.GetAsync("/api/community/feed?limit=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Ensure controller normalized the limit and the mediator received Limit == 50
        fake.LastGetFeedQuery.Should().NotBeNull();
        fake.LastGetFeedQuery!.Limit.Should().Be(50);
    }

    [Fact]
    public async Task GetPost_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        await using var app = await BuildAppAsync(new FakeMediator());
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, " ");

        var response = await client.GetAsync("/api/community/posts/10");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPost_ReturnsOk_WithPostFromMediator()
    {
        var fake = new FakeMediator();
        fake.GetPostResponse = new PostDTO
        {
            Id = 10,
            AuthorId = "author-1",
            Content = "Hello",
            PostType = PostType.Story,
            PostVisibility = PostVisibility.Public,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-1),
            IsEdited = true
        };

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "user-1", new[] { "Doctor" });

        var response = await client.GetAsync("/api/community/posts/10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PostDTO>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be(10);
        payload.IsEdited.Should().BeTrue();
        payload.UpdatedAt.Should().BeCloseTo(fake.GetPostResponse.UpdatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetPost_ReturnsForbidden_WhenVisibilityBlocked()
    {
        var fake = new FakeMediator
        {
            GetPostException = new PostAccessForbiddenException("blocked")
        };

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "user-1", new[] { "Patient" });

        var response = await client.GetAsync("/api/community/posts/10");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdatePost_ReturnsOk_WithUpdatedPost()
    {
        var fake = new FakeMediator();
        var updatedAt = DateTime.UtcNow;
        fake.UpdatePostResponse = new PostDTO
        {
            Id = 12,
            AuthorId = "user-1",
            Content = "Updated",
            PostType = PostType.Question,
            PostVisibility = PostVisibility.CaregiverOnly,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = updatedAt,
            IsEdited = true
        };

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "user-1");

        var request = new UpdatePostDTO { Content = "Updated", Visibility = PostVisibility.CaregiverOnly };
        var response = await client.PutAsJsonAsync("/api/community/posts/12", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        fake.LastUpdatePostCommand.Should().NotBeNull();
        fake.LastUpdatePostCommand!.PostId.Should().Be(12);
        fake.LastUpdatePostCommand.Post.Content.Should().Be("Updated");
        fake.LastUpdatePostCommand.Post.Visibility.Should().Be(PostVisibility.CaregiverOnly);
        var payload = await response.Content.ReadFromJsonAsync<PostDTO>();
        payload.Should().NotBeNull();
        payload!.IsEdited.Should().BeTrue();
        payload.UpdatedAt.Should().BeCloseTo(updatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdatePost_ReturnsForbidden_WhenNotAuthor()
    {
        var fake = new FakeMediator
        {
            UpdatePostException = new PostAccessForbiddenException("forbidden")
        };

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "user-2");

        var request = new UpdatePostDTO { Content = "Updated", Visibility = PostVisibility.Public };
        var response = await client.PutAsJsonAsync("/api/community/posts/12", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeletePost_ReturnsNoContent_WhenAuthor()
    {
        var fake = new FakeMediator();
        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "user-1");

        var response = await client.DeleteAsync("/api/community/posts/20");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        fake.LastDeletePostCommand.Should().NotBeNull();
        fake.LastDeletePostCommand!.PostId.Should().Be(20);
        fake.LastDeletePostCommand.RequesterId.Should().Be("user-1");
    }

    [Fact]
    public async Task DeletePost_ReturnsNoContent_WhenModerator()
    {
        var fake = new FakeMediator();
        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "moderator-1", new[] { "MODERATOR" });

        var response = await client.DeleteAsync("/api/community/posts/21");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        fake.LastDeletePostCommand.Should().NotBeNull();
        fake.LastDeletePostCommand!.Roles.Should().Contain("MODERATOR");
    }

    [Fact]
    public async Task AddReaction_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        await using var app = await BuildAppAsync(new FakeMediator());
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, " ");

        var response = await client.PostAsync("/api/community/posts/10/reactions?type=Like", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddReaction_ReturnsOk_WhenSuccessful()
    {
        var fake = new FakeMediator();
        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "user-1");

        var response = await client.PostAsync("/api/community/posts/10/reactions?type=Like", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        fake.LastAddReactionCommand.Should().NotBeNull();
        fake.LastAddReactionCommand!.PostId.Should().Be(10);
        fake.LastAddReactionCommand.Type.Should().Be(ReactionType.Like);
        fake.LastAddReactionCommand.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task AddReaction_ReturnsConflict_WhenDuplicateReaction()
    {
        var fake = new FakeMediator
        {
            AddReactionException = new DuplicateReactionException("Conflict")
        };
        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "user-1");

        var response = await client.PostAsync("/api/community/posts/10/reactions?type=Support", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddReaction_ReturnsForbidden_WhenVisibilityBlocked()
    {
        var fake = new FakeMediator
        {
            AddReactionException = new PostAccessForbiddenException("Blocked")
        };
        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "user-1");

        var response = await client.PostAsync("/api/community/posts/10/reactions?type=Like", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddReaction_ReturnsNotFound_WhenPostDoesNotExist()
    {
        var fake = new FakeMediator
        {
            AddReactionException = new PostNotFoundException("Not Found")
        };
        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "user-1");

        var response = await client.PostAsync("/api/community/posts/999/reactions?type=Like", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static HttpClient CreateAuthenticatedClient(WebApplication app, string userId, IReadOnlyCollection<string>? roles = null)
    {
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        if (roles is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(',', roles));
        }
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

    private sealed class FakeMediator : IMediator
    {
        public GetFeedQuery? LastGetFeedQuery { get; private set; }
        public GetPostQuery? LastGetPostQuery { get; private set; }
        public UpdatePostCommand? LastUpdatePostCommand { get; private set; }
        public DeletePostCommand? LastDeletePostCommand { get; private set; }
        public AddReactionCommand? LastAddReactionCommand { get; private set; }
        public Exception? AddReactionException { get; set; }
        public FeedResponseDto Response { get; set; } = new FeedResponseDto
        {
            Posts = new List<PostDTO>
            {
                new() { Id = 1 },
                new() { Id = 2 },
                new() { Id = 3 }
            },
            NextCursor = 3
        };
        public PostDTO GetPostResponse { get; set; } = new PostDTO
        {
            Id = 1,
            AuthorId = "author-1",
            Content = "content",
            PostType = PostType.Story,
            PostVisibility = PostVisibility.Public,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-10),
            IsEdited = false
        };
        public PostDTO UpdatePostResponse { get; set; } = new PostDTO
        {
            Id = 1,
            AuthorId = "author-1",
            Content = "content",
            PostType = PostType.Story,
            PostVisibility = PostVisibility.Public,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-5),
            IsEdited = true
        };
        public Exception? GetPostException { get; set; }
        public Exception? UpdatePostException { get; set; }
        public Exception? DeletePostException { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is GetFeedQuery q)
            {
                LastGetFeedQuery = q;
                return Task.FromResult((TResponse)(object)Response);
            }

            if (request is GetPostQuery getPostQuery)
            {
                LastGetPostQuery = getPostQuery;
                if (GetPostException is not null)
                {
                    return Task.FromException<TResponse>(GetPostException);
                }

                return Task.FromResult((TResponse)(object)GetPostResponse);
            }

            if (request is UpdatePostCommand updatePostCommand)
            {
                LastUpdatePostCommand = updatePostCommand;
                if (UpdatePostException is not null)
                {
                    return Task.FromException<TResponse>(UpdatePostException);
                }

                return Task.FromResult((TResponse)(object)UpdatePostResponse);
            }

            if (request is DeletePostCommand deletePostCommand)
            {
                LastDeletePostCommand = deletePostCommand;
                if (DeletePostException is not null)
                {
                    return Task.FromException<TResponse>(DeletePostException);
                }

                return Task.FromResult((TResponse)(object)MediatR.Unit.Value);
            }

            if (request is AddReactionCommand addReactionCommand)
            {
                LastAddReactionCommand = addReactionCommand;
                if (AddReactionException is not null)
                {
                    return Task.FromException<TResponse>(AddReactionException);
                }
                return Task.FromResult((TResponse)(object)MediatR.Unit.Value);
            }

            return Task.FromResult(default(TResponse)!);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            if (request is GetFeedQuery q)
            {
                LastGetFeedQuery = q;
                return Task.FromResult<object?>(Response);
            }

            if (request is GetPostQuery getPostQuery)
            {
                LastGetPostQuery = getPostQuery;
                if (GetPostException is not null)
                {
                    return Task.FromException<object?>(GetPostException);
                }

                return Task.FromResult<object?>(GetPostResponse);
            }

            if (request is UpdatePostCommand updatePostCommand)
            {
                LastUpdatePostCommand = updatePostCommand;
                if (UpdatePostException is not null)
                {
                    return Task.FromException<object?>(UpdatePostException);
                }

                return Task.FromResult<object?>(UpdatePostResponse);
            }

            if (request is DeletePostCommand deletePostCommand)
            {
                LastDeletePostCommand = deletePostCommand;
                if (DeletePostException is not null)
                {
                    return Task.FromException<object?>(DeletePostException);
                }

                return Task.FromResult<object?>(MediatR.Unit.Value);
            }

            if (request is AddReactionCommand addReactionCommandObj)
            {
                LastAddReactionCommand = addReactionCommandObj;
                if (AddReactionException is not null)
                {
                    return Task.FromException<object?>(AddReactionException);
                }
                return Task.FromResult<object?>(MediatR.Unit.Value);
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