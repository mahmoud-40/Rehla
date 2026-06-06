using System.Globalization;
using BreastCancer.Community.Features.Queries.GetFollowing;
using BreastCancer.Context;
using BreastCancer.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BreastCancer.Tests.Unit;

public class GetFollowingQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithNoFollowing_ReturnsEmptyList()
    {
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new BreastCancerDB(options);
        var handler = new GetFollowingQueryHandler(context);
        var query = new GetFollowingQuery("user-1", null, 20);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Followers.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithFollowing_ReturnsPaginatedResult()
    {
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var context = new BreastCancerDB(options))
        {
            // Setup data
            var now = DateTime.UtcNow;
            var following = new List<Follow>
            {
                new() { FollowerId = "user-1", FollowingId = "following-1", CreatedAt = now.AddMinutes(-30) },
                new() { FollowerId = "user-1", FollowingId = "following-2", CreatedAt = now.AddMinutes(-20) },
                new() { FollowerId = "user-1", FollowingId = "following-3", CreatedAt = now.AddMinutes(-10) }
            };

            var users = new List<ApplicationUser>
            {
                new() { Id = "user-1", FirstName = "User", LastName = "One", Email = "user1@example.com" },
                new() { Id = "following-1", FirstName = "Following", LastName = "One", Email = "following1@example.com" },
                new() { Id = "following-2", FirstName = "Following", LastName = "Two", Email = "following2@example.com" },
                new() { Id = "following-3", FirstName = "Following", LastName = "Three", Email = "following3@example.com" }
            };

            context.Users.AddRange(users);
            context.Follows.AddRange(following);
            await context.SaveChangesAsync();
        }

        using (var context = new BreastCancerDB(options))
        {
            var handler = new GetFollowingQueryHandler(context);
            var query = new GetFollowingQuery("user-1", null, 20);

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
    public async Task Handle_WithCursor_ReturnsFollowingAfterCursor()
    {
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var context = new BreastCancerDB(options))
        {
            var now = DateTime.UtcNow;
            var cursorTime = now.AddMinutes(-20);

            var following = new List<Follow>
            {
                new() { FollowerId = "user-1", FollowingId = "following-1", CreatedAt = now.AddMinutes(-30) },
                new() { FollowerId = "user-1", FollowingId = "following-2", CreatedAt = cursorTime },
                new() { FollowerId = "user-1", FollowingId = "following-3", CreatedAt = now.AddMinutes(-10) }
            };

            var users = new List<ApplicationUser>
            {
                new() { Id = "user-1", FirstName = "User", LastName = "One", Email = "user1@example.com" },
                new() { Id = "following-1", FirstName = "Following", LastName = "One", Email = "following1@example.com" },
                new() { Id = "following-2", FirstName = "Following", LastName = "Two", Email = "following2@example.com" },
                new() { Id = "following-3", FirstName = "Following", LastName = "Three", Email = "following3@example.com" }
            };

            context.Users.AddRange(users);
            context.Follows.AddRange(following);
            await context.SaveChangesAsync();
        }

        using (var context = new BreastCancerDB(options))
        {
            var handler = new GetFollowingQueryHandler(context);
            var cursor = DateTime.UtcNow.AddMinutes(-20).ToString("o");
            var query = new GetFollowingQuery("user-1", cursor, 20);

            var result = await handler.Handle(query, CancellationToken.None);

            result.Followers.Should().HaveCount(1);
            result.Total.Should().Be(3);
        }
    }

    [Fact]
    public async Task Handle_WithLimitExceeded_ReturnsNextCursor()
    {
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var context = new BreastCancerDB(options))
        {
            var now = DateTime.UtcNow;
            var following = new List<Follow>
            {
                new() { FollowerId = "user-1", FollowingId = "following-1", CreatedAt = now.AddMinutes(-30) },
                new() { FollowerId = "user-1", FollowingId = "following-2", CreatedAt = now.AddMinutes(-20) },
                new() { FollowerId = "user-1", FollowingId = "following-3", CreatedAt = now.AddMinutes(-10) }
            };

            var users = new List<ApplicationUser>
            {
                new() { Id = "user-1", FirstName = "User", LastName = "One", Email = "user1@example.com" },
                new() { Id = "following-1", FirstName = "Following", LastName = "One", Email = "following1@example.com" },
                new() { Id = "following-2", FirstName = "Following", LastName = "Two", Email = "following2@example.com" },
                new() { Id = "following-3", FirstName = "Following", LastName = "Three", Email = "following3@example.com" }
            };

            context.Users.AddRange(users);
            context.Follows.AddRange(following);
            await context.SaveChangesAsync();
        }

        using (var context = new BreastCancerDB(options))
        {
            var handler = new GetFollowingQueryHandler(context);
            var query = new GetFollowingQuery("user-1", null, 2); // limit to 2

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
            var handler = new GetFollowingQueryHandler(context);
            
            // Test with limit > 100
            var query1 = new GetFollowingQuery("user-1", null, 150);
            var result1 = await handler.Handle(query1, CancellationToken.None);
            result1.Followers.Should().BeEmpty();

            // Test with limit < 1
            var query2 = new GetFollowingQuery("user-1", null, -5);
            var result2 = await handler.Handle(query2, CancellationToken.None);
            result2.Followers.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Handle_WithInvalidCursor_IgnoresCursor()
    {
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var context = new BreastCancerDB(options))
        {
            var now = DateTime.UtcNow;
            var following = new List<Follow>
            {
                new() { FollowerId = "user-1", FollowingId = "following-1", CreatedAt = now.AddMinutes(-30) },
                new() { FollowerId = "user-1", FollowingId = "following-2", CreatedAt = now.AddMinutes(-20) }
            };

            var users = new List<ApplicationUser>
            {
                new() { Id = "user-1", FirstName = "User", LastName = "One", Email = "user1@example.com" },
                new() { Id = "following-1", FirstName = "Following", LastName = "One", Email = "following1@example.com" },
                new() { Id = "following-2", FirstName = "Following", LastName = "Two", Email = "following2@example.com" }
            };

            context.Users.AddRange(users);
            context.Follows.AddRange(following);
            await context.SaveChangesAsync();
        }

        using (var context = new BreastCancerDB(options))
        {
            var handler = new GetFollowingQueryHandler(context);
            var query = new GetFollowingQuery("user-1", "invalid-cursor", 20);

            var result = await handler.Handle(query, CancellationToken.None);

            result.Followers.Should().HaveCount(2);
            result.Total.Should().Be(2);
        }
    }
}
