using System.Net;
using System.Net.Http.Json;
using System.Collections.Generic;
using BreastCancer.Community.Controllers;
using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Features.Feed;
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
        fake.Response = new FeedResponseDto { PostIds = new List<int> { 10, 20, 30 }, NextCursor = 30 };

        await using var app = await BuildAppAsync(fake);
        using var client = CreateAuthenticatedClient(app, "user-1");

        var response = await client.GetAsync("/api/community/feed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<FeedResponseDto>();
        payload.Should().NotBeNull();
        payload!.PostIds.Should().ContainInOrder(new[] { 10, 20, 30 });
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

    private sealed class FakeMediator : IMediator
    {
        public GetFeedQuery? LastGetFeedQuery { get; private set; }
        public FeedResponseDto Response { get; set; } = new FeedResponseDto { PostIds = new List<int> { 1, 2, 3 }, NextCursor = 3 };

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is GetFeedQuery q)
            {
                LastGetFeedQuery = q;
                return Task.FromResult((TResponse)(object)Response);
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
