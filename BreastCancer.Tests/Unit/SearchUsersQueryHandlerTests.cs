using BreastCancer.Community.Features.Queries.SearchUsers;
using BreastCancer.Context;
using BreastCancer.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BreastCancer.Tests.Unit;

public class SearchUsersQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var context = new BreastCancerDB(options))
        {
            context.Users.Add(new ApplicationUser { Id = "1", FirstName = "John", LastName = "Doe", Email = "john@test.com" });
            await context.SaveChangesAsync();
        }

        using (var context = new BreastCancerDB(options))
        {
            var handler = new SearchUsersQueryHandler(context);
            var query = new SearchUsersQuery("Zebra", 20);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Handle_SearchByName_ReturnsMatchingUsers_SortedByFirstName()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var context = new BreastCancerDB(options))
        {
            context.Users.AddRange(new List<ApplicationUser>
            {
                new() { Id = "1", FirstName = "Alice", LastName = "Smith", Email = "alice@test.com" },
                new() { Id = "2", FirstName = "Bob", LastName = "Smith", Email = "bob@test.com" },
                new() { Id = "3", FirstName = "Charlie", LastName = "Brown", Email = "charlie@test.com" }
            });
            await context.SaveChangesAsync();
        }

        using (var context = new BreastCancerDB(options))
        {
            var handler = new SearchUsersQueryHandler(context);
            var query = new SearchUsersQuery("Smith", 20); 

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            result.Select(u => u.Name).Should().ContainInOrder("Alice Smith", "Bob Smith");
        }
    }

    [Fact]
    public async Task Handle_SearchByEmail_ReturnsMatchingUser()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var context = new BreastCancerDB(options))
        {
            context.Users.AddRange(new List<ApplicationUser>
            {
                new() { Id = "1", FirstName = "John", LastName = "Doe", Email = "john.doe@hospital.com" },
                new() { Id = "2", FirstName = "Jane", LastName = "Doe", Email = "jane.doe@clinic.com" }
            });
            await context.SaveChangesAsync();
        }

        using (var context = new BreastCancerDB(options))
        {
            var handler = new SearchUsersQueryHandler(context);
            var query = new SearchUsersQuery("hospital.com", 20); 

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().ContainSingle();
            result.First().Email.Should().Be("john.doe@hospital.com");
        }
    }

    [Fact]
    public async Task Handle_SearchByExactId_ReturnsMatchingUser()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var context = new BreastCancerDB(options))
        {
            context.Users.AddRange(new List<ApplicationUser>
            {
                new() { Id = "user-123", FirstName = "Test", LastName = "User", Email = "test@test.com" },
                new() { Id = "user-456", FirstName = "Another", LastName = "User", Email = "another@test.com" }
            });
            await context.SaveChangesAsync();
        }

        using (var context = new BreastCancerDB(options))
        {
            var handler = new SearchUsersQueryHandler(context);
            var query = new SearchUsersQuery("user-456", 20); 

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().ContainSingle();
            result.First().Id.Should().Be("user-456");
        }
    }

    [Fact]
    public async Task Handle_WithLimitExceeded_ReturnsOnlyLimitedAmount()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var context = new BreastCancerDB(options))
        {
            var users = Enumerable.Range(1, 5).Select(i => 
                new ApplicationUser { Id = i.ToString(), FirstName = "Clone", LastName = "Trooper", Email = $"clone{i}@test.com" }
            );
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
        }

        using (var context = new BreastCancerDB(options))
        {
            var handler = new SearchUsersQueryHandler(context);
            var query = new SearchUsersQuery("Clone", 2); 

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
        }
    }
}