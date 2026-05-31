using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Features.Posts;
using BreastCancer.Community.Services.Interface;
using BreastCancer.Context;
using BreastCancer.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BreastCancer.Tests.Unit;

public sealed class DeletePostCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenPostMissing_ThrowsNotFound()
    {
        await using var dbContext = CreateDbContext();
        var cache = new Mock<ICacheService>();
        var handler = new DeletePostCommandHandler(dbContext, cache.Object);

        var command = new DeletePostCommand(1, "user-1", new[] { "Patient" });

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<PostNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenNotAuthorOrModerator_ThrowsForbidden()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Posts.Add(new Post { Id = 1, AuthorId = "author-1", Content = "x" });
        await dbContext.SaveChangesAsync();

        var cache = new Mock<ICacheService>();
        var handler = new DeletePostCommandHandler(dbContext, cache.Object);

        var command = new DeletePostCommand(1, "user-2", new[] { "Patient" });

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<PostAccessForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenModerator_DeletesAndInvalidatesCache()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Posts.Add(new Post { Id = 1, AuthorId = "author-1", Content = "x" });
        await dbContext.SaveChangesAsync();

        var cache = new Mock<ICacheService>();
        var handler = new DeletePostCommandHandler(dbContext, cache.Object);

        var command = new DeletePostCommand(1, "moderator-1", new[] { "MODERATOR" });

        await handler.Handle(command, CancellationToken.None);

        var post = await dbContext.Posts.IgnoreQueryFilters().FirstAsync();
        post.IsDeleted.Should().BeTrue();
        cache.Verify(c => c.DeleteAsync("post:1", It.IsAny<CancellationToken>()), Times.Once);
    }

    private static BreastCancerDB CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BreastCancerDB>()
            .UseInMemoryDatabase($"delete-post-{Guid.NewGuid()}")
            .Options;
        return new BreastCancerDB(options);
    }
}
