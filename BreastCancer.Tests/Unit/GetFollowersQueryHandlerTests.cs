using System.Globalization;
using BreastCancer.Community.Features.Queries.GetFollowers;
using BreastCancer.Context;
using BreastCancer.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Rehla.Community.Features.Queries.GetFollowers;
using Xunit;

namespace BreastCancer.Tests.Unit;

public class GetFollowersQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithNoFollowers_ReturnsEmptyList()
    {
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new BreastCancerDB(options);
        var handler = new GetFollowersQueryHandler(context, new MockUserManager());
        var query = new GetFollowersQuery("user-1", null, 20);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Followers.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithFollowers_ReturnsPaginatedResult()
    {
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var context = new BreastCancerDB(options))
        {
            // Setup data
            var now = DateTime.UtcNow;
            var followers = new List<Follow>
            {
                new() { FollowerId = "follower-1", FollowingId = "user-1", CreatedAt = now.AddMinutes(-30) },
                new() { FollowerId = "follower-2", FollowingId = "user-1", CreatedAt = now.AddMinutes(-20) },
                new() { FollowerId = "follower-3", FollowingId = "user-1", CreatedAt = now.AddMinutes(-10) }
            };

            var users = new List<ApplicationUser>
            {
                new() { Id = "user-1", FirstName = "User", LastName = "One", Email = "user1@example.com" },
                new() { Id = "follower-1", FirstName = "Follower", LastName = "One", Email = "follower1@example.com" },
                new() { Id = "follower-2", FirstName = "Follower", LastName = "Two", Email = "follower2@example.com" },
                new() { Id = "follower-3", FirstName = "Follower", LastName = "Three", Email = "follower3@example.com" }
            };

            context.Users.AddRange(users);
            context.Follows.AddRange(followers);
            await context.SaveChangesAsync();
        }

        using (var context = new BreastCancerDB(options))
        {
            var handler = new GetFollowersQueryHandler(context, new MockUserManager());
            var query = new GetFollowersQuery("user-1", null, 20);

            var result = await handler.Handle(query, CancellationToken.None);

            result.Followers.Should().HaveCount(3);
            result.Total.Should().Be(3);
            result.NextCursor.Should().BeNull();
            result.Followers.Should().AllSatisfy(f =>
            {
                f.UserId.Should().NotBeNullOrEmpty();
                f.Name.Should().NotBeNullOrEmpty();
                f.Role.Should().NotBeNullOrEmpty();
            });
        }
    }

    [Fact]
    public async Task Handle_WithCursor_ReturnsPaginatedFollowers()
    {
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var context = new BreastCancerDB(options))
        {
            var now = DateTime.UtcNow;
            var cursorTime = now.AddMinutes(-20);

            var followers = new List<Follow>
            {
                new() { FollowerId = "follower-1", FollowingId = "user-1", CreatedAt = now.AddMinutes(-30) },
                new() { FollowerId = "follower-2", FollowingId = "user-1", CreatedAt = cursorTime },
                new() { FollowerId = "follower-3", FollowingId = "user-1", CreatedAt = now.AddMinutes(-10) }
            };

            var users = new List<ApplicationUser>
            {
                new() { Id = "user-1", FirstName = "User", LastName = "One", Email = "user1@example.com" },
                new() { Id = "follower-1", FirstName = "Follower", LastName = "One", Email = "follower1@example.com" },
                new() { Id = "follower-2", FirstName = "Follower", LastName = "Two", Email = "follower2@example.com" },
                new() { Id = "follower-3", FirstName = "Follower", LastName = "Three", Email = "follower3@example.com" }
            };

            context.Users.AddRange(users);
            context.Follows.AddRange(followers);
            await context.SaveChangesAsync();
        }

        using (var context = new BreastCancerDB(options))
        {
            var handler = new GetFollowersQueryHandler(context, new MockUserManager());
            var cursor = DateTime.UtcNow.AddMinutes(-20).ToString("o");
            var query = new GetFollowersQuery("user-1", cursor, 20);

            var result = await handler.Handle(query, CancellationToken.None);

            result.Followers.Should().HaveCount(1);
            result.Total.Should().Be(3);
        }
    }

    [Fact]
    public async Task Handle_WithLimitExceeded_ReturnNextCursor()
    {
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var context = new BreastCancerDB(options))
        {
            var now = DateTime.UtcNow;
            var followers = new List<Follow>
            {
                new() { FollowerId = "follower-1", FollowingId = "user-1", CreatedAt = now.AddMinutes(-30) },
                new() { FollowerId = "follower-2", FollowingId = "user-1", CreatedAt = now.AddMinutes(-20) },
                new() { FollowerId = "follower-3", FollowingId = "user-1", CreatedAt = now.AddMinutes(-10) }
            };

            var users = new List<ApplicationUser>
            {
                new() { Id = "user-1", FirstName = "User", LastName = "One", Email = "user1@example.com" },
                new() { Id = "follower-1", FirstName = "Follower", LastName = "One", Email = "follower1@example.com" },
                new() { Id = "follower-2", FirstName = "Follower", LastName = "Two", Email = "follower2@example.com" },
                new() { Id = "follower-3", FirstName = "Follower", LastName = "Three", Email = "follower3@example.com" }
            };

            context.Users.AddRange(users);
            context.Follows.AddRange(followers);
            await context.SaveChangesAsync();
        }

        using (var context = new BreastCancerDB(options))
        {
            var handler = new GetFollowersQueryHandler(context, new MockUserManager());
            var query = new GetFollowersQuery("user-1", null, 2); // limit to 2

            var result = await handler.Handle(query, CancellationToken.None);

            result.Followers.Should().HaveCount(2);
            result.Total.Should().Be(3);
            result.NextCursor.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task Handle_ClampsLimit_Between1And100()
    {
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var context = new BreastCancerDB(options))
        {
            var handler = new GetFollowersQueryHandler(context, new MockUserManager());
            
            // Test with limit > 100
            var query1 = new GetFollowersQuery("user-1", null, 150);
            var result1 = await handler.Handle(query1, CancellationToken.None);
            result1.Followers.Should().BeEmpty();

            // Test with limit < 1
            var query2 = new GetFollowersQuery("user-1", null, -5);
            var result2 = await handler.Handle(query2, CancellationToken.None);
            result2.Followers.Should().BeEmpty();
        }
    }

    // Helper class for mocking UserManager
    private class MockUserManager : Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>
    {
        public MockUserManager() : base(
            new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser>(
                new BreastCancerDB(new DbContextOptionsBuilder<BreastCancerDB>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)),
            null, null, null, null, null, null, null, null)
        {
        }
    }
}
