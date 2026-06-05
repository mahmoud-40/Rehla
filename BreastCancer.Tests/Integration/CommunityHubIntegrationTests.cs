using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BreastCancer.Community.Hubs;
using BreastCancer.Community.Services.Implementation;
using BreastCancer.Community.Services.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BreastCancer.Tests.Integration
{
    public class CommunityHubIntegrationTests
    {
        [Fact]
        public async Task NewPostAvailable_OnlySentToTargetUserGroup()
        {
            // Arrange
            await using var app = await BuildAppAsync();

            var clientA = BuildHubConnection(app, "user-a-id");
            var clientB = BuildHubConnection(app, "user-b-id");

            var receivedByA = new List<string>();
            var receivedByB = new List<string>();
            var tcsA = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            clientA.On<string>(CommunityHub.NewPostAvailableMethod, postId =>
            {
                receivedByA.Add(postId);
                tcsA.TrySetResult(postId);
            });

            clientB.On<string>(CommunityHub.NewPostAvailableMethod, postId =>
            {
                receivedByB.Add(postId);
            });

            await clientA.StartAsync();
            await clientB.StartAsync();

            // Act
            var notifier = app.Services.GetRequiredService<ICommunityNotifier>();
            await notifier.NotifyNewPostAsync("user-a-id", "post-42");

            // Assert
            var result = await Task.WhenAny(tcsA.Task, Task.Delay(3000));
            Assert.Equal(tcsA.Task, result);
            Assert.Equal("post-42", tcsA.Task.Result);

            await Task.Delay(500);
            Assert.Empty(receivedByB);

            await clientA.StopAsync();
            await clientB.StopAsync();
        }

        private static HubConnection BuildHubConnection(WebApplication app, string userId) =>
            new HubConnectionBuilder()
                .WithUrl(app.GetTestServer().BaseAddress + "hubs/community", opts =>
                {
                    opts.HttpMessageHandlerFactory = _ => app.GetTestServer().CreateHandler();
                    opts.Headers.Add(TestAuthHandler.UserIdHeader, userId);
                })
                .Build();

        private static async Task<WebApplication> BuildAppAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();

            builder.Services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            builder.Services.AddAuthorization();

            builder.Services.AddSignalR();
            builder.Services.AddScoped<ICommunityNotifier, CommunityNotifier>();

            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<CommunityHub>("/hubs/community");

            await app.StartAsync();
            return app;
        }
    }
}